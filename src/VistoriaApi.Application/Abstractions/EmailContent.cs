using System.Collections.Generic;

namespace VistoriaApi.Application.Abstractions;

public sealed class EmailContent
{
    public string Subject { get; init; }
    public string HtmlBody { get; init; }
    public string TextBody { get; init; }
    public IReadOnlyCollection<InlineResource> InlineResources { get; init; }

    public EmailContent(
        string subject,
        string htmlBody,
        string textBody,
        IEnumerable<InlineResource>? inlineResources = null)
    {
        Subject = subject ?? string.Empty;
        HtmlBody = htmlBody ?? string.Empty;
        TextBody = textBody ?? string.Empty;
        InlineResources = inlineResources is null
            ? Array.Empty<InlineResource>()
            : inlineResources.ToList().AsReadOnly();
    }
}

public sealed class InlineResource
{
    public string ContentId { get; init; }
    public string FilePath { get; init; }
    public string MediaType { get; init; }

    public InlineResource(string contentId, string filePath, string mediaType)
    {
        ContentId = contentId;
        FilePath = filePath;
        MediaType = mediaType;
    }
}
