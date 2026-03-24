using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BIMConcierge.Api.Services;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IConfiguration config, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _config["Resend:ApiKey"]);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string customerName, string licenseKey, string plan, string? tempPassword)
    {
        var resendSection = _config.GetSection("Resend");
        var fromAddress = resendSection["FromAddress"] ?? "noreply@bimconcierge.io";
        var fromName = resendSection["FromName"] ?? "BIMConcierge";

        var payload = new
        {
            from = $"{fromName} <{fromAddress}>",
            to = new[] { toEmail },
            subject = $"Bem-vindo ao BIMConcierge — Sua licença {plan}",
            html = BuildWelcomeHtml(customerName, licenseKey, plan, tempPassword)
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("emails", content);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Welcome email sent to {Email} via Resend", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email} via Resend", toEmail);
            throw;
        }
    }

    private static string BuildWelcomeHtml(string customerName, string licenseKey, string plan, string? tempPassword)
    {
        var passwordBlock = tempPassword is not null
            ? $"""
              <h3 style="color: #333;">Sua senha temporária:</h3>
              <div style="background: #fff3e0; border: 2px dashed #ff9800; border-radius: 8px; padding: 20px; text-align: center; margin: 24px 0;">
                <span style="font-size: 20px; font-weight: bold; letter-spacing: 2px; color: #e65100; font-family: 'Courier New', monospace;">
                  {tempPassword}
                </span>
              </div>
              <p style="color: #555; line-height: 1.6;">
                <strong>Importante:</strong> altere sua senha após o primeiro login.
              </p>
              """
            : "";

        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8" /></head>
        <body style="font-family: 'Segoe UI', Arial, sans-serif; background: #f4f6f9; padding: 40px 0;">
          <div style="max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,0.08);">
            <div style="background: linear-gradient(135deg, #1a73e8, #0d47a1); padding: 32px; text-align: center;">
              <h1 style="color: #fff; margin: 0; font-size: 28px;">BIMConcierge</h1>
              <p style="color: #bbdefb; margin: 8px 0 0;">Seu assistente inteligente para Revit</p>
            </div>
            <div style="padding: 32px;">
              <h2 style="color: #333;">Olá, {customerName}! 👋</h2>
              <p style="color: #555; line-height: 1.6;">
                Sua assinatura <strong>{plan}</strong> está ativa. Abaixo está sua chave de licença para ativar o plugin no Revit:
              </p>
              <div style="background: #e8f5e9; border: 2px dashed #4caf50; border-radius: 8px; padding: 20px; text-align: center; margin: 24px 0;">
                <span style="font-size: 24px; font-weight: bold; letter-spacing: 3px; color: #2e7d32; font-family: 'Courier New', monospace;">
                  {licenseKey}
                </span>
              </div>
              {passwordBlock}
              <h3 style="color: #333;">Como ativar:</h3>
              <ol style="color: #555; line-height: 1.8;">
                <li>Abra o Revit 2026</li>
                <li>Clique na aba <strong>BIMConcierge</strong> na Ribbon</li>
                <li>Clique em <strong>Login</strong></li>
                <li>Cole a chave acima no campo "Chave de Licença"</li>
                <li>Pronto! Comece a usar.</li>
              </ol>
              <h3 style="color: #333;">O que você ganha:</h3>
              <ul style="color: #555; line-height: 1.8;">
                <li>✅ Tutoriais guiados passo a passo dentro do Revit</li>
                <li>✅ Correção em tempo real de erros de modelagem</li>
                <li>✅ Padrões da empresa aplicados automaticamente</li>
                <li>✅ Dashboard de progresso e gamificação</li>
                <li>✅ Suporte prioritário</li>
              </ul>
              <p style="color: #999; font-size: 13px; margin-top: 32px; border-top: 1px solid #eee; padding-top: 16px;">
                Se precisar de ajuda, responda este e-mail ou acesse nosso suporte.<br/>
                © {DateTime.UtcNow.Year} BIMConcierge. Todos os direitos reservados.
              </p>
            </div>
          </div>
        </body>
        </html>
        """;
    }
}
