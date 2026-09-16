using System.ComponentModel.DataAnnotations;

namespace VistoriaApi.Application.Contracts;

public sealed class PhotoRequirementRequest
{
    [Required]
    [StringLength(60, MinimumLength = 1)]
    [RegularExpression(
        @"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "O código deve conter apenas letras, números, hífen e underscore.")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Label { get; init; } = string.Empty;

    public bool Required { get; init; }

    [Range(0, 999)]
    public int SortOrder { get; init; }
}

public sealed class CreateTemplateRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string Category { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Required]
    public List<PhotoRequirementRequest> PhotoRequirements { get; init; } = new();
}
