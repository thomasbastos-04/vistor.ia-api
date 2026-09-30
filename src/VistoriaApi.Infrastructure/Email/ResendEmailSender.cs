using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VistoriaApi.Application.Abstractions;

namespace VistoriaApi.Infrastructure.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailSender> _logger;
    private const string LogoContentId = "vistoria-logo";

    public ResendEmailSender(
        IHttpClientFactory httpClientFactory,
        IEmailTemplateRenderer renderer,
        IConfiguration configuration,
        ILogger<ResendEmailSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _renderer = renderer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendInspectionInviteAsync(
        string email,
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        ValidateRecipient(email, recipientName);

        var primaryColor = GetColor("Email:ColorPrimary", "#123F36");
        var secondaryColor = GetColor("Email:ColorSecondary", "#E2AA3B");

        var logoPath = _configuration["Email:LogoPath"];
        var logoExists = !string.IsNullOrWhiteSpace(logoPath) && File.Exists(Path.GetFullPath(logoPath));
        var logoContentId = logoExists ? LogoContentId : null;

        var content = await _renderer.RenderInspectionInviteAsync(
            recipientName,
            publicUrl,
            expiresAtUtc,
            logoContentId,
            primaryColor,
            secondaryColor,
            cancellationToken);

        var html = content.HtmlBody;

        if (logoExists)
        {
            try
            {
                var fullPath = Path.GetFullPath(logoPath!);
                var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                var base64 = Convert.ToBase64String(bytes);
                var dataUri = $"data:image/png;base64,{base64}";
                html = html.Replace($"cid:{LogoContentId}", dataUri, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao processar logo para envio por Resend. Envio seguirá sem imagem.");
            }
        }

        await SendAsync(email, content.Subject, html, content.TextBody, cancellationToken);
    }

    public async Task SendVerificationCodeAsync(
        string email,
        string recipientName,
        string code,
        CancellationToken cancellationToken)
    {
        ValidateRecipient(email, recipientName);

        var primaryColor = GetColor("Email:ColorPrimary", "#123F36");
        var secondaryColor = GetColor("Email:ColorSecondary", "#E2AA3B");

        var logoPath = _configuration["Email:LogoPath"];
        var logoExists = !string.IsNullOrWhiteSpace(logoPath) && File.Exists(Path.GetFullPath(logoPath));
        var logoContentId = logoExists ? LogoContentId : null;

        var content = await _renderer.RenderVerificationCodeAsync(
            recipientName,
            code,
            logoContentId,
            primaryColor,
            secondaryColor,
            cancellationToken);

        var html = content.HtmlBody;

        if (logoExists)
        {
            try
            {
                var fullPath = Path.GetFullPath(logoPath!);
                var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                var base64 = Convert.ToBase64String(bytes);
                var dataUri = $"data:image/png;base64,{base64}";
                html = html.Replace($"cid:{LogoContentId}", dataUri, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao processar logo para envio por Resend. Envio seguirá sem imagem.");
            }
        }

        await SendAsync(email, content.Subject, html, content.TextBody, cancellationToken);
    }

    public async Task SendWelcomeAsync(
        string email,
        string recipientName,
        CancellationToken cancellationToken)
    {
        ValidateRecipient(email, recipientName);

        var primaryColor = GetColor("Email:ColorPrimary", "#123F36");
        var secondaryColor = GetColor("Email:ColorSecondary", "#E2AA3B");

        var logoPath = _configuration["Email:LogoPath"];
        var logoExists = !string.IsNullOrWhiteSpace(logoPath) && File.Exists(Path.GetFullPath(logoPath));
        var logoContentId = logoExists ? LogoContentId : null;

        var content = await _renderer.RenderWelcomeAsync(
            recipientName,
            logoContentId,
            primaryColor,
            secondaryColor,
            cancellationToken);

        var html = content.HtmlBody;

        if (logoExists)
        {
            try
            {
                var fullPath = Path.GetFullPath(logoPath!);
                var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                var base64 = Convert.ToBase64String(bytes);
                var dataUri = $"data:image/png;base64,{base64}";
                html = html.Replace($"cid:{LogoContentId}", dataUri, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao processar logo para envio por Resend. Envio seguirá sem imagem.");
            }
        }

        await SendAsync(email, content.Subject, html, content.TextBody, cancellationToken);
    }

    private async Task SendAsync(string to, string subject, string html, string text, CancellationToken cancellationToken)
    {
        var from = _configuration["Resend:From"];
        if (string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("A configuração Resend:From não foi informada.");
        }

        var client = _httpClientFactory.CreateClient("resend");

        var payload = new
        {
            from,
            to = new[] { to },
            subject,
            html,
            text
        };

        HttpResponseMessage? response = null;

        try
        {
            _logger.LogInformation("Enviando e-mail do tipo {Subject} para {Recipient}", subject, to);

            response = await client.PostAsJsonAsync("/emails", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Falha ao enviar e-mail via Resend. Status: {Status}, Body: {Body}", (int)response.StatusCode, Truncate(body, 1000));
                response.EnsureSuccessStatusCode();
            }

            _logger.LogInformation("E-mail enviado com sucesso via Resend para {Recipient}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail via Resend para {Recipient}", to);
            throw;
        }
        finally
        {
            response?.Dispose();
        }
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length <= maxLength) return value;
        return value.Substring(0, maxLength);
    }

    private void ValidateRecipient(string email, string recipientName)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email");
        if (string.IsNullOrWhiteSpace(recipientName)) throw new ArgumentException("recipientName");
    }

    private string GetColor(string configurationKey, string fallback)
    {
        var configuredColor = _configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(configuredColor)) return fallback;
        return configuredColor.Trim();
    }
}
