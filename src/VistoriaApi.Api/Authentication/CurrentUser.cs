using System.Security.Claims;
using VistoriaApi.Application.Abstractions;

namespace VistoriaApi.Api.Authentication;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var subject = _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(subject, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("Usuário não autenticado.");
        }
    }
}
