using System.ComponentModel.DataAnnotations;
using VistoriaApi.Domain.Enums;

namespace VistoriaApi.Application.Contracts;

public sealed class AuthResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }

    public AuthResponse(Guid id, string name, string email, string accessToken, DateTime expiresAtUtc)
    {
        Id = id;
        Name = name;
        Email = email;
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
    }
}

public sealed class CreateInspectionRequest
{
    [Required]
    public Guid TemplateId { get; init; }

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string RecipientName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string RecipientEmail { get; init; } = string.Empty;

    [StringLength(200)]
    public string? AssetIdentification { get; init; }

    [Range(1, 365)]
    public int LinkValidDays { get; init; } = 7;
}

public sealed class SendInspectionRequest
{
    [Required]
    public string PublicToken { get; init; } = string.Empty;
}

public sealed class CreatedInspectionResponse
{
    public Guid Id { get; init; }
    public string PublicUrl { get; init; } = string.Empty;
    public string PublicToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }

    public CreatedInspectionResponse(Guid id, string publicUrl, string publicToken, DateTime expiresAtUtc)
    {
        Id = id;
        PublicUrl = publicUrl;
        PublicToken = publicToken;
        ExpiresAtUtc = expiresAtUtc;
    }
}

public sealed class InspectionSummaryResponse
{
    public Guid Id { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string RecipientEmail { get; init; } = string.Empty;
    public string? AssetIdentification { get; init; }
    public InspectionStatus Status { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }

    public InspectionSummaryResponse(Guid id, string templateName, string recipientName, string recipientEmail, string? assetIdentification, InspectionStatus status, DateTime expiresAtUtc, DateTime createdAtUtc, DateTime? completedAtUtc)
    {
        Id = id;
        TemplateName = templateName;
        RecipientName = recipientName;
        RecipientEmail = recipientEmail;
        AssetIdentification = assetIdentification;
        Status = status;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        CompletedAtUtc = completedAtUtc;
    }
}

public sealed class PublicRequirementResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool Required { get; init; }
    public int SortOrder { get; init; }
    public bool Uploaded { get; init; }

    public PublicRequirementResponse(Guid id, string code, string label, bool required, int sortOrder, bool uploaded)
    {
        Id = id;
        Code = code;
        Label = label;
        Required = required;
        SortOrder = sortOrder;
        Uploaded = uploaded;
    }
}

public sealed class PublicInspectionResponse
{
    public Guid Id { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string? AssetIdentification { get; init; }
    public InspectionStatus Status { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public List<PublicRequirementResponse> Requirements { get; init; } = new();

    public PublicInspectionResponse(Guid id, string templateName, string category, string recipientName, string? assetIdentification, InspectionStatus status, DateTime expiresAtUtc, List<PublicRequirementResponse> requirements)
    {
        Id = id;
        TemplateName = templateName;
        Category = category;
        RecipientName = recipientName;
        AssetIdentification = assetIdentification;
        Status = status;
        ExpiresAtUtc = expiresAtUtc;
        Requirements = requirements;
    }
}

public sealed class TemplateRequirementResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool Required { get; init; }
    public int SortOrder { get; init; }

    public TemplateRequirementResponse(Guid id, string code, string label, bool required, int sortOrder)
    {
        Id = id;
        Code = code;
        Label = label;
        Required = required;
        SortOrder = sortOrder;
    }
}

public sealed class TemplateResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Active { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public List<TemplateRequirementResponse> Requirements { get; init; } = new();

    public TemplateResponse(Guid id, string name, string category, string? description, bool active, DateTime createdAtUtc, IEnumerable<TemplateRequirementResponse> requirements)
    {
        Id = id;
        Name = name;
        Category = category;
        Description = description;
        Active = active;
        CreatedAtUtc = createdAtUtc;
        Requirements = requirements.ToList();
    }
}

public sealed class CompleteInspectionRequest
{
    [StringLength(2000)]
    public string? Notes { get; init; }
}
