namespace VistoriaApi.Application.Abstractions;

public interface IEmailSender
{
    Task SendInspectionInviteAsync(
        string email,
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);
}
