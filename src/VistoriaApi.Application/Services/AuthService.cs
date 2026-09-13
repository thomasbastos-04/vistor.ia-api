using Microsoft.EntityFrameworkCore;
using VistoriaApi.Application.Abstractions;
using VistoriaApi.Application.Contracts;
using VistoriaApi.Application.Exceptions;
using VistoriaApi.Domain.Entities;

namespace VistoriaApi.Application.Services;

public sealed class AuthService
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthService(
        IAppDbContext dbContext,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailAlreadyExists = await _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailAlreadyExists)
        {
            throw new AppException("E-mail já cadastrado.", 409);
        }

        var user = new User(
            request.Name,
            normalizedEmail,
            _passwordService.Hash(request.Password));

        _dbContext.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            item => item.Email == normalizedEmail,
            cancellationToken);

        if (user is null || !_passwordService.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException("E-mail ou senha inválidos.", 401);
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email,
            accessToken.Token,
            accessToken.ExpiresAtUtc);
    }
}
