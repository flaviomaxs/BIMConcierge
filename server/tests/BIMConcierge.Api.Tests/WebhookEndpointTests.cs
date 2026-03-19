using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BIMConcierge.Api.Data;
using BIMConcierge.Api.Entities;
using BIMConcierge.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Api.Tests;

/// <summary>
/// Fake email sender that records calls instead of sending real emails.
/// </summary>
public class FakeEmailSender : IEmailSender
{
    public List<(string Email, string Name, string Key, string Plan)> SentEmails { get; } = [];

    public Task SendWelcomeEmailAsync(string toEmail, string customerName, string licenseKey, string plan)
    {
        SentEmails.Add((toEmail, customerName, licenseKey, plan));
        return Task.CompletedTask;
    }
}

public class WebhookApiFactory : WebApplicationFactory<Program>
{
    public readonly FakeEmailSender FakeEmail = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace DB with InMemory
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            var dbName = "TestDb_Webhook_" + Guid.NewGuid();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Replace email sender with fake
            var emailDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailSender));
            if (emailDescriptor is not null) services.Remove(emailDescriptor);
            services.AddSingleton<IEmailSender>(FakeEmail);
        });
    }
}

public class WebhookEndpointTests : IClassFixture<WebhookApiFactory>
{
    private readonly HttpClient _client;
    private readonly WebhookApiFactory _factory;

    public WebhookEndpointTests(WebhookApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string BuildStripeEvent(string? email, string? name = "Test User",
        string plan = "Professional", int amount = 14990, string paymentId = "pi_test_001") =>
        JsonSerializer.Serialize(new
        {
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = paymentId,
                    customer_email = email,
                    customer_name = name,
                    amount_total = amount,
                    currency = "brl",
                    payment_status = "paid",
                    metadata = new { plan, max_seats = "5" }
                }
            }
        });

    private async Task<HttpResponseMessage> PostWebhook(string body)
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await _client.PostAsync("/v1/webhooks/stripe", content);
    }

    [Fact]
    public async Task Webhook_ValidPayment_CreatesLicenseAndUser()
    {
        var body = BuildStripeEvent("novo@empresa.com", "Maria Silva", "Professional", paymentId: "pi_new_001");
        var response = await PostWebhook(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Provisioned successfully", json.GetProperty("message").GetString());

        var licenseKey = json.GetProperty("licenseKey").GetString();
        Assert.NotNull(licenseKey);
        Assert.StartsWith("BIM-", licenseKey);

        var userId = json.GetProperty("userId").GetString();
        Assert.False(string.IsNullOrEmpty(userId));

        var companyId = json.GetProperty("companyId").GetString();
        Assert.False(string.IsNullOrEmpty(companyId));

        // Verify email was sent
        Assert.Contains(_factory.FakeEmail.SentEmails,
            e => e.Email == "novo@empresa.com" && e.Plan == "Professional" && e.Key == licenseKey);
    }

    [Fact]
    public async Task Webhook_DuplicatePaymentId_IsIdempotent()
    {
        var paymentId = "pi_dup_001";
        var body = BuildStripeEvent("dup@empresa.com", "Dup User", paymentId: paymentId);

        var response1 = await PostWebhook(body);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var json1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Provisioned successfully", json1.GetProperty("message").GetString());

        // Second call with same paymentId should return "Already processed"
        var response2 = await PostWebhook(body);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var json2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Already processed", json2.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Webhook_InvalidSignature_Returns401()
    {
        // Configure a webhook secret so signature validation is enforced
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                             || d.ServiceType == typeof(DbContextOptions))
                    .ToList();
                foreach (var d in descriptors) services.Remove(d);

                var sigDbName = "TestDb_Webhook_Sig_" + Guid.NewGuid();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(sigDbName));

                var emailDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor is not null) services.Remove(emailDescriptor);
                services.AddSingleton<IEmailSender>(new FakeEmailSender());
            });

            builder.UseSetting("Stripe:WebhookSecret", "whsec_test_secret_123");
        });

        var client = factory.CreateClient();
        var body = BuildStripeEvent("test@empresa.com");
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", "t=123,v1=invalidsignature");

        var response = await client.PostAsync("/v1/webhooks/stripe", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_MissingEmail_Returns400()
    {
        var body = BuildStripeEvent(email: null);
        var response = await PostWebhook(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_ExistingUser_CreatesNewLicenseOnly()
    {
        // First, create a user via a webhook
        var body1 = BuildStripeEvent("existing@empresa.com", "Carlos", "Trial", paymentId: "pi_exist_001");
        var response1 = await PostWebhook(body1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var json1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var companyId1 = json1.GetProperty("companyId").GetString();
        var userId1 = json1.GetProperty("userId").GetString();

        // Second webhook for same email — should reuse same user and company
        var body2 = BuildStripeEvent("existing@empresa.com", "Carlos", "Professional", paymentId: "pi_exist_002");
        var response2 = await PostWebhook(body2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var json2 = await response2.Content.ReadFromJsonAsync<JsonElement>();

        // Same user and company
        Assert.Equal(companyId1, json2.GetProperty("companyId").GetString());
        Assert.Equal(userId1, json2.GetProperty("userId").GetString());

        // Different license keys
        Assert.NotEqual(
            json1.GetProperty("licenseKey").GetString(),
            json2.GetProperty("licenseKey").GetString());
    }

    [Fact]
    public async Task PublicPlans_ReturnsAllPlans()
    {
        var response = await _client.GetAsync("/v1/public/plans");
        response.EnsureSuccessStatusCode();

        var plans = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, plans.GetArrayLength());

        var planNames = new List<string>();
        foreach (var plan in plans.EnumerateArray())
            planNames.Add(plan.GetProperty("Plan").GetString()!);

        Assert.Contains("Trial", planNames);
        Assert.Contains("Professional", planNames);
        Assert.Contains("Enterprise", planNames);
    }
}
