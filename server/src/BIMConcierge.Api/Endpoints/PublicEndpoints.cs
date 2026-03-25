using System.Net.Http.Headers;
using System.Text;
using BIMConcierge.Api.Data;
using BIMConcierge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Endpoints;

public static class PublicEndpoints
{
    public static RouteGroupBuilder MapPublicEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/plans", GetPlans);
        group.MapGet("/config", GetConfig);
        group.MapPost("/checkout", CreateCheckout);
        group.MapPost("/trial", CreateTrial);
        return group;
    }

    private static IResult GetPlans()
    {
        var plans = new[]
        {
            new PlanInfo("Trial", 0m, "BRL", 1, 14, [
                "Tutoriais guiados passo a passo",
                "Correção em tempo real de erros",
                "5 padrões da empresa",
                "Dashboard de progresso"
            ]),
            new PlanInfo("Solo", 79.90m, "BRL", 1, 365, [
                "Tudo do Trial",
                "Padrões ilimitados da empresa",
                "Dashboard de progresso completo",
                "Suporte por email"
            ], 297.90m),
            new PlanInfo("Team", 149.90m, "BRL", 3, 365, [
                "Tudo do Solo",
                "Até 3 seats",
                "Gamificação + conquistas",
                "Suporte prioritário"
            ], 697.90m),
            new PlanInfo("Professional", 249.90m, "BRL", 5, 365, [
                "Tudo do Team",
                "Até 5 seats",
                "Dashboard de progresso completo",
                "Gamificação + conquistas",
                "Suporte prioritário"
            ], 997.90m),
            new PlanInfo("Enterprise", 699.90m, "BRL", 50, 365, [
                "Tudo do Professional",
                "Até 50 seats",
                "Gamificação + ranking entre equipes",
                "API customizada",
                "Onboarding dedicado",
                "Suporte 24/7"
            ], 4997.90m)
        };

        return Results.Ok(plans);
    }

    private static IResult GetConfig(IConfiguration config)
    {
        return Results.Ok(new
        {
            StripePublishableKey = config["Stripe:PublishableKey"] ?? ""
        });
    }

    private static async Task<IResult> CreateCheckout(
        CheckoutRequest request,
        IConfiguration config,
        HttpContext context)
    {
        var secretKey = config["Stripe:SecretKey"] ?? "";
        if (string.IsNullOrEmpty(secretKey))
            return Results.BadRequest(new { error = "Stripe not configured" });

        if (string.IsNullOrEmpty(request.Email))
            return Results.BadRequest(new { error = "Email is required" });

        if (string.IsNullOrEmpty(request.Plan))
            return Results.BadRequest(new { error = "Plan is required" });

        // Map plan to Stripe Price ID (production)
        var (priceId, seats) = request.Plan.ToLowerInvariant() switch
        {
            "solo" => ("price_1TEuQmFFewmBK9f2YQ9hkwYV", 1),
            "team" => ("price_1TEuROFFewmBK9f2r5ona8ZJ", 3),
            "enterprise" => ("price_1TEaKjFFewmBK9f2ubTUNSza", 50),
            _ => ("price_1TEaK2FFewmBK9f22AXDAMe5", 5) // Professional
        };

        // Trial doesn't need payment
        if (request.Plan.Equals("trial", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "Trial plan does not require payment" });

        var origin = $"{context.Request.Scheme}://{context.Request.Host}";

        // Create Stripe Checkout Session via REST API
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:")));

        var formData = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["customer_email"] = request.Email,
            ["line_items[0][price]"] = priceId,
            ["line_items[0][quantity]"] = "1",
            ["metadata[plan]"] = request.Plan,
            ["metadata[max_seats]"] = seats.ToString(),
            ["success_url"] = $"{origin}/sucesso?session_id={{CHECKOUT_SESSION_ID}}",
            ["cancel_url"] = $"{origin}/#planos"
        };

        var response = await httpClient.PostAsync(
            "https://api.stripe.com/v1/checkout/sessions",
            new FormUrlEncodedContent(formData));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return Results.Problem($"Stripe error: {error}", statusCode: 502);
        }

        var json = await response.Content.ReadFromJsonAsync<StripeCheckoutResponse>();
        return Results.Ok(new { CheckoutUrl = json?.Url ?? "" });
    }

    private static async Task<IResult> CreateTrial(
        TrialRequest request,
        AppDbContext db,
        ProvisioningService provisioning)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "Email é obrigatório" });

        var email = request.Email.Trim().ToLowerInvariant();

        // Check if this email already has an active Trial license
        var hasActiveTrial = await db.Users
            .Where(u => u.Email == email)
            .SelectMany(u => db.Licenses.Where(l => l.CompanyId == u.CompanyId))
            .AnyAsync(l => l.Type == "Trial" && l.ExpiresAt > DateTime.UtcNow);

        if (hasActiveTrial)
            return Results.BadRequest(new { error = "Já existe um Trial ativo para este email" });

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? email.Split('@')[0]
            : request.Name.Trim();

        var result = await provisioning.ProvisionAsync(email, name, "Trial", 1, Guid.NewGuid().ToString());

        return Results.Ok(new
        {
            message = "Trial ativado com sucesso",
            licenseKey = result.LicenseKey,
            userId = result.UserId,
            companyId = result.CompanyId
        });
    }
}

public record PlanInfo(string Plan, decimal Price, string Currency, int Seats, int DurationDays, string[] Features, decimal? OriginalPrice = null);
public record CheckoutRequest(string Plan, string Email);
public record TrialRequest(string Email, string? Name);

internal record StripeCheckoutResponse(string? Id, string? Url);
