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
}
