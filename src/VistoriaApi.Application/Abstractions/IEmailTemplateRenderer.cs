namespace VistoriaApi.Application.Abstractions;

public interface IEmailTemplateRenderer
{
    Task<EmailContent> RenderInspectionInviteAsync(
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc,
        string? logoContentId,
        string primaryColor,
        string secondaryColor,
        CancellationToken cancellationToken);

    Task<EmailContent> RenderVerificationCodeAsync(
        string recipientName,
        string code,
        string? logoContentId,
        string primaryColor,
        string secondaryColor,
        CancellationToken cancellationToken);

    Task<EmailContent> RenderWelcomeAsync(
        string recipientName,
        string? logoContentId,
        string primaryColor,
        string secondaryColor,
        CancellationToken cancellationToken);
}
