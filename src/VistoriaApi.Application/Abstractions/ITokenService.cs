using VistoriaApi.Domain.Entities;

namespace VistoriaApi.Application.Abstractions;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user);

    string GeneratePublicToken();

    string HashPublicToken(string token);
}
