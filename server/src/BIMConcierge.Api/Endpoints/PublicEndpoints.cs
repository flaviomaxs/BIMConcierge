using System.Net.Http.Headers;
using System.Text;

namespace BIMConcierge.Api.Endpoints;

public static class PublicEndpoints
{
    public static RouteGroupBuilder MapPublicEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/plans", GetPlans);
        group.MapGet("/config", GetConfig);
        group.MapPost("/checkout", CreateCheckout);
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
            new PlanInfo("Professional", 149.90m, "BRL", 5, 365, [
                "Tudo do Trial",
                "Padrões ilimitados da empresa",
                "Dashboard de progresso completo",
                "Gamificação + conquistas",
                "Suporte prioritário"
            ]),
            new PlanInfo("Enterprise", 499.90m, "BRL", 50, 365, [
                "Tudo do Professional",
                "Até 50 seats",
                "Gamificação + ranking entre equipes",
                "API customizada",
                "Onboarding dedicado",
                "Suporte 24/7"
            ])
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

        // Map plan to Stripe price (in centavos)
        var (priceAmount, seats) = request.Plan.ToLowerInvariant() switch
        {
            "trial" => (0, 1),
            "enterprise" => (49990, 50),
            _ => (14990, 5) // Professional
        };

        // Trial doesn't need payment
        if (priceAmount == 0)
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
            ["line_items[0][price_data][currency]"] = "brl",
            ["line_items[0][price_data][product_data][name]"] = $"BIM Concierge {request.Plan}",
            ["line_items[0][price_data][product_data][description]"] = $"Licença {request.Plan} — {seats} seats, 1 ano",
            ["line_items[0][price_data][unit_amount]"] = priceAmount.ToString(),
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
}

public record PlanInfo(string Plan, decimal Price, string Currency, int Seats, int DurationDays, string[] Features);
public record CheckoutRequest(string Plan, string Email);

internal record StripeCheckoutResponse(string? Id, string? Url);
