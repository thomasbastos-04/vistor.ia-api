using System.ComponentModel.DataAnnotations;
using VistoriaApi.Domain.Enums;

namespace VistoriaApi.Application.Contracts;

public sealed record CreateInspectionRequest(
    [property: Required] Guid TemplateId,
    [property: Required, StringLength(120)] string RecipientName,
    [property: Required, EmailAddress, StringLength(180)] string RecipientEmail,
    [property: Required, StringLength(120)] string AssetIdentification,
    [property: Range(1, 30)] int LinkValidDays = 7);

public sealed record SendInspectionRequest(
    [property: Required] string PublicToken);

public sealed record CompleteInspectionRequest(
    [property: StringLength(2000)] string? Notes);

public sealed record CreatedInspectionResponse(
    Guid Id,
    string PublicUrl,
    string PublicToken,
    DateTime ExpiresAtUtc);

public sealed record InspectionSummaryResponse(
    Guid Id,
    string TemplateName,
    string RecipientName,
    string RecipientEmail,
    string AssetIdentification,
    InspectionStatus Status,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record PublicRequirementResponse(
    Guid Id,
    string Code,
    string Label,
    bool Required,
    int SortOrder,
    bool Uploaded);

public sealed record PublicInspectionResponse(
    Guid Id,
    string TemplateName,
    string Category,
    string RecipientName,
    string AssetIdentification,
    InspectionStatus Status,
    DateTime ExpiresAtUtc,
    IEnumerable<PublicRequirementResponse> Requirements);
