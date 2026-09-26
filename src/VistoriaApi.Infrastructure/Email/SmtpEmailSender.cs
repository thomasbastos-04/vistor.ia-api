using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MimeKit;
using VistoriaApi.Application.Abstractions;

namespace VistoriaApi.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private const string DefaultPrimaryColor = "#123F36";
    private const string DefaultSecondaryColor = "#E2AA3B";
    private const string DefaultBackgroundColor = "#F5F7F6";

    private readonly IConfiguration _configuration;
    private readonly IEmailTemplateRenderer _renderer;

    public SmtpEmailSender(IConfiguration configuration, IEmailTemplateRenderer renderer)
    {
        _configuration = configuration;
        _renderer = renderer;
    }

    public async Task SendInspectionInviteAsync(
        string email,
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        ValidateRecipient(email, recipientName);
        ValidatePublicUrl(publicUrl);

        var primaryColor = GetColor("Email:ColorPrimary", DefaultPrimaryColor);
        var secondaryColor = GetColor("Email:ColorSecondary", DefaultSecondaryColor);

        var builder = new BodyBuilder();
        var logoContentId = TryAddInlineLogo(builder);

        var content = await _renderer.RenderInspectionInviteAsync(
            recipientName,
            publicUrl,
            expiresAtUtc,
            logoContentId,
            primaryColor,
            secondaryColor,
            cancellationToken);

        builder.HtmlBody = content.HtmlBody;
        builder.TextBody = content.TextBody;

        var settings = GetSmtpSettings();
        var message = BuildMimeMessage(content, settings.From, recipientName, email, builder);

        await SmtpClientHelper.SendAsync(message, settings, cancellationToken);
    }

    public async Task SendVerificationCodeAsync(
        string email,
        string recipientName,
        string code,
        CancellationToken cancellationToken)
    {
        ValidateRecipient(email, recipientName);

        var primaryColor = GetColor("Email:ColorPrimary", DefaultPrimaryColor);
        var secondaryColor = GetColor("Email:ColorSecondary", DefaultSecondaryColor);

        var builder = new BodyBuilder();
        var logoContentId = TryAddInlineLogo(builder);

        var content = await _renderer.RenderVerificationCodeAsync(
            recipientName,
            code,
            logoContentId,
            primaryColor,
            secondaryColor,
            cancellationToken);

        builder.HtmlBody = content.HtmlBody;
        builder.TextBody = content.TextBody;

        var settings = GetSmtpSettings();
        var message = BuildMimeMessage(content, settings.From, recipientName, email, builder);

        await SmtpClientHelper.SendAsync(message, settings, cancellationToken);
    }

    public async Task SendWelcomeAsync(
        string email,
        string recipientName,
        CancellationToken cancellationToken)
    {
        ValidateRecipient(email, recipientName);

        var primaryColor = GetColor("Email:ColorPrimary", DefaultPrimaryColor);
        var secondaryColor = GetColor("Email:ColorSecondary", DefaultSecondaryColor);

        var builder = new BodyBuilder();
        var logoContentId = TryAddInlineLogo(builder);

        var content = await _renderer.RenderWelcomeAsync(
            recipientName,
            logoContentId,
            primaryColor,
            secondaryColor,
            cancellationToken);

        builder.HtmlBody = content.HtmlBody;
        builder.TextBody = content.TextBody;

        var settings = GetSmtpSettings();
        var message = BuildMimeMessage(content, settings.From, recipientName, email, builder);

        await SmtpClientHelper.SendAsync(message, settings, cancellationToken);
    }

    private string? TryAddInlineLogo(BodyBuilder builder)
    {
        var configuredPath = _configuration["Email:LogoPath"];

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(configuredPath);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            var extension = Path.GetExtension(fullPath);

            // SVG não é confiável em Gmail e Outlook.
            if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var logo = builder.LinkedResources.Add(fullPath);
            logo.ContentId = "vistoria-logo";

            return logo.ContentId;
        }
        catch (Exception exception)
            when (exception is IOException
                  or UnauthorizedAccessException
                  or NotSupportedException
                  or ArgumentException)
        {
            return null;
        }
    }

    private SmtpSettings GetSmtpSettings()
    {
        var host = GetRequiredConfiguration("Smtp:Host");
        var port = _configuration.GetValue<int>("Smtp:Port");

        if (port <= 0)
        {
            throw new InvalidOperationException("A configuração Smtp:Port é inválida.");
        }

        var from = GetRequiredConfiguration("Smtp:From");
        var useSsl = _configuration.GetValue<bool>("Smtp:UseSsl");
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];

        return new SmtpSettings
        {
            Host = host,
            Port = port,
            UseSsl = useSsl,
            From = from,
            Username = username,
            Password = password
        };
    }

    private MimeMessage BuildMimeMessage(
        EmailContent content,
        string senderEmail,
        string recipientName,
        string recipientEmail,
        BodyBuilder builder)
    {
        var message = new MimeMessage
        {
            Subject = content.Subject,
            Body = builder.ToMessageBody()
        };

        message.From.Add(new MailboxAddress("Vistor.ia", senderEmail));
        message.To.Add(new MailboxAddress(recipientName.Trim(), recipientEmail.Trim()));

        return message;
    }

    private string GetRequiredConfiguration(string key)
    {
        var value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"A configuração obrigatória '{key}' não foi informada.");
        }

        return value.Trim();
    }

    private string GetColor(string configurationKey, string fallback)
    {
        var configuredColor = _configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(configuredColor)) return fallback;
        var color = configuredColor.Trim();
        if (color.Length != 7 || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit)) return fallback;
        return color.ToUpperInvariant();
    }
    private static string FormatExpiration(DateTime expiresAtUtc)
    {
        var utcExpiration = expiresAtUtc.Kind == DateTimeKind.Local
            ? expiresAtUtc.ToUniversalTime()
            : DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);

        return $"{utcExpiration:dd/MM/yyyy 'às' HH:mm} UTC";
    }

    private static void ValidateRecipient(string email, string recipientName)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("O e-mail do destinatário é obrigatório.", nameof(email));
        if (string.IsNullOrWhiteSpace(recipientName)) throw new ArgumentException("O nome do destinatário é obrigatório.", nameof(recipientName));
        try { _ = MailboxAddress.Parse(email); } catch (ParseException ex) { throw new ArgumentException("O e-mail do destinatário é inválido.", nameof(email), ex); }
    }

    private static void ValidatePublicUrl(string publicUrl)
    {
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("O endereço público da vistoria é inválido.", nameof(publicUrl));
        }
    }
}
