using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using VistoriaApi.Application.Abstractions;

namespace VistoriaApi.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendInspectionInviteAsync(
        string email,
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_configuration["Smtp:From"]));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Você recebeu uma vistoria";
        message.Body = new TextPart("html")
        {
            Text = BuildEmailBody(recipientName, publicUrl, expiresAtUtc)
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            _configuration.GetValue<int>("Smtp:Port"),
            _configuration.GetValue<bool>("Smtp:UseSsl"),
            cancellationToken);

        var username = _configuration["Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(
                username,
                _configuration["Smtp:Password"],
                cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static string BuildEmailBody(
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc)
    {
        var safeName = System.Net.WebUtility.HtmlEncode(recipientName);
        var safeUrl = System.Net.WebUtility.HtmlEncode(publicUrl);

        return $"""
            <p>Olá, {safeName}.</p>
            <p>Você recebeu uma solicitação de vistoria.</p>
            <p><a href="{safeUrl}">Realizar vistoria</a></p>
            <p>Este link é válido até {expiresAtUtc:dd/MM/yyyy HH:mm} UTC.</p>
            """;
    }
}
