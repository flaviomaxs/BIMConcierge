using System.Net;
using System.Net.Mail;

namespace BIMConcierge.Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string customerName, string licenseKey, string plan)
    {
        var emailSection = _config.GetSection("Email");
        var host = emailSection["Host"] ?? "smtp.gmail.com";
        var port = int.Parse(emailSection["Port"] ?? "587");
        var username = emailSection["Username"] ?? "";
        var password = emailSection["Password"] ?? "";
        var fromAddress = emailSection["FromAddress"] ?? "noreply@bimconcierge.io";
        var fromName = emailSection["FromName"] ?? "BIM Concierge";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = $"Bem-vindo ao BIM Concierge — Sua licença {plan}",
            IsBodyHtml = true,
            Body = BuildWelcomeHtml(customerName, licenseKey, plan)
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Welcome email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            throw;
        }
    }

    private static string BuildWelcomeHtml(string customerName, string licenseKey, string plan) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8" /></head>
        <body style="font-family: 'Segoe UI', Arial, sans-serif; background: #f4f6f9; padding: 40px 0;">
          <div style="max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,0.08);">
            <div style="background: linear-gradient(135deg, #1a73e8, #0d47a1); padding: 32px; text-align: center;">
              <h1 style="color: #fff; margin: 0; font-size: 28px;">BIM Concierge</h1>
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
              <h3 style="color: #333;">Como ativar:</h3>
              <ol style="color: #555; line-height: 1.8;">
                <li>Abra o Revit 2026</li>
                <li>Clique na aba <strong>BIM Concierge</strong> na Ribbon</li>
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
                © {DateTime.UtcNow.Year} BIM Concierge. Todos os direitos reservados.
              </p>
            </div>
          </div>
        </body>
        </html>
        """;
}
