using MailKit.Net.Smtp;
using MimeKit;

namespace VistoriaApi.Infrastructure.Email;

public static class SmtpClientHelper
{
    public static async Task SendAsync(
        MimeMessage message,
        SmtpSettings settings,
        CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                settings.Host,
                settings.Port,
                settings.UseSsl,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                var password = settings.Password ?? string.Empty;
                await client.AuthenticateAsync(
                    settings.Username,
                    password,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, CancellationToken.None);
            }
        }
    }
}
