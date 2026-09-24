namespace VistoriaApi.Application.Abstractions;

public interface IEmailSender
{
    Task SendInspectionInviteAsync(
        string email,
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);

    Task SendVerificationCodeAsync(
        string email,
        string recipientName,
        string code,
        CancellationToken cancellationToken);

    Task SendWelcomeAsync(
        string email,
        string recipientName,
        CancellationToken cancellationToken);
}
