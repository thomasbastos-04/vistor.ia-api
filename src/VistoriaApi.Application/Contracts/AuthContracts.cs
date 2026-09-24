using System.ComponentModel.DataAnnotations;

namespace VistoriaApi.Application.Contracts;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public sealed class VerifyEmailRequest
{
    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 4)]
    public string Code { get; init; } = string.Empty;
}
