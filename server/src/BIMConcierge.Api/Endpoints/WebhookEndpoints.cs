using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BIMConcierge.Api.Data;
using BIMConcierge.Api.Entities;
using BIMConcierge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Endpoints;

public static class WebhookEndpoints
{
    public static RouteGroupBuilder MapWebhookEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/stripe", HandleStripeWebhook);
        return group;
    }

    private static async Task<IResult> HandleStripeWebhook(
        HttpContext context,
        AppDbContext db,
        ProvisioningService provisioning,
        IConfiguration config)
    {
        // Read raw body for signature verification
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Validate Stripe signature
        var signatureHeader = context.Request.Headers["Stripe-Signature"].FirstOrDefault();
        var webhookSecret = config["Stripe:WebhookSecret"] ?? "";

        if (!string.IsNullOrEmpty(webhookSecret) && !VerifyStripeSignature(body, signatureHeader, webhookSecret))
            return Results.Json(new { error = "Invalid signature" }, statusCode: 401);

        // Parse event
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch
        {
            return Results.BadRequest(new { error = "Invalid JSON" });
        }

        var root = doc.RootElement;
        var eventType = root.GetProperty("type").GetString();

        if (eventType != "checkout.session.completed")
            return Results.Ok(new { message = $"Event {eventType} ignored" });

        var session = root.GetProperty("data").GetProperty("object");

        // Extract fields
        var email = GetStringOrNull(session, "customer_email");
        if (string.IsNullOrEmpty(email))
            return Results.BadRequest(new { error = "Missing customer_email" });

        var customerName = GetStringOrNull(session, "customer_name") ?? email.Split('@')[0];
        var paymentId = GetStringOrNull(session, "id") ?? "";
        var amountTotal = session.TryGetProperty("amount_total", out var amt) ? amt.GetInt32() : 0;
        var currency = GetStringOrNull(session, "currency") ?? "brl";

        // Extract plan from metadata
        var plan = "Professional";
        var maxSeats = 5;
        if (session.TryGetProperty("metadata", out var metadata))
        {
            plan = GetStringOrNull(metadata, "plan") ?? "Professional";
            if (metadata.TryGetProperty("max_seats", out var seatsEl))
                int.TryParse(seatsEl.GetString(), out maxSeats);
        }

        // Idempotency: check if this payment was already processed
        var existingOrder = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == paymentId);
        if (existingOrder is not null && existingOrder.Status == "Completed")
            return Results.Ok(new { message = "Already processed", licenseKey = existingOrder.LicenseId });

        // Create order record
        if (existingOrder is null)
        {
            var order = new OrderEntity
            {
                Email = email,
                CustomerName = customerName,
                Plan = plan,
                MaxSeats = maxSeats,
                PriceAmount = amountTotal / 100m,
                Currency = currency.ToUpperInvariant(),
                PaymentProvider = "Stripe",
                PaymentId = paymentId,
                Status = "Pending"
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        // Provision company + user + license + email
        var result = await provisioning.ProvisionAsync(email, customerName, plan, maxSeats, paymentId);

        return Results.Ok(new
        {
            message = "Provisioned successfully",
            licenseKey = result.LicenseKey,
            userId = result.UserId,
            companyId = result.CompanyId
        });
    }

    private static bool VerifyStripeSignature(string payload, string? signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(signatureHeader)) return false;

        // Parse "t=timestamp,v1=signature" format
        string? timestamp = null;
        string? v1Signature = null;

        foreach (var part in signatureHeader.Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0] == "t") timestamp = kv[1];
            if (kv[0] == "v1") v1Signature = kv[1];
        }

        if (timestamp is null || v1Signature is null) return false;

        // Compute expected signature: HMAC-SHA256(secret, "timestamp.payload")
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(v1Signature));
    }

    private static string? GetStringOrNull(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }
}
