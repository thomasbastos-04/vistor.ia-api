using System.ComponentModel.DataAnnotations;

namespace VistoriaApi.Application.Contracts;

public sealed record PhotoRequirementRequest(
    [property: Required, RegularExpression("^[a-zA-Z0-9_-]+$")] string Code,
    [property: Required, StringLength(100)] string Label,
    bool Required,
    [property: Range(0, 999)] int SortOrder);

public sealed record CreateTemplateRequest(
    [property: Required, StringLength(120)] string Name,
    [property: Required, StringLength(80)] string Category,
    [property: StringLength(500)] string? Description,
    [property: Required, MinLength(1)] List<PhotoRequirementRequest> PhotoRequirements);
