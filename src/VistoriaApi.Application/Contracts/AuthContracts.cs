using System.ComponentModel.DataAnnotations;

namespace VistoriaApi.Application.Contracts;

public sealed record RegisterRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Required, EmailAddress, StringLength(180)] string Email,
    [property: Required, StringLength(100, MinimumLength = 8)] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record AuthResponse(
    Guid UserId,
    string Name,
    string Email,
    string AccessToken,
    DateTime ExpiresAtUtc);
