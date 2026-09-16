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
