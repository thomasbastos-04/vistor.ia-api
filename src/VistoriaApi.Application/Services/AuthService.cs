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
    private readonly IEmailSender _emailSender;

    public AuthService(
        IAppDbContext dbContext,
        IPasswordService passwordService,
        ITokenService tokenService,
        IEmailSender emailSender)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailSender = emailSender;
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

        // Gera código de verificação
        var code = Random.Shared.Next(100000, 999999).ToString();
        var expires = DateTime.UtcNow.AddMinutes(15);

        user.SetEmailVerification(code, expires);

        _dbContext.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Envia e-mail de verificação
        try
        {
            await _emailSender.SendVerificationCodeAsync(
                user.Email,
                user.Name,
                code,
                cancellationToken);
        }
        catch
        {
            // Não falhar o cadastro se envio de e-mail falhar; registrar e prosseguir.
        }

        return CreateAuthResponse(user);
    }

    public async Task VerifyEmailAsync(string email, string code, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            item => item.Email == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            throw new AppException("Usuário não encontrado.", 404);
        }

        if (user.IsEmailVerified)
        {
            return;
        }

        if (user.EmailVerificationCode is null || user.EmailVerificationExpiresAtUtc is null)
        {
            throw new AppException("Código de verificação inválido.", 400);
        }

        if (!string.Equals(user.EmailVerificationCode, code, StringComparison.OrdinalIgnoreCase) ||
            user.EmailVerificationExpiresAtUtc.Value < DateTime.UtcNow)
        {
            throw new AppException("Código de verificação inválido ou expirado.", 400);
        }

        user.MarkEmailAsVerified();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Envia e-mail de boas-vindas
        try
        {
            await _emailSender.SendWelcomeAsync(user.Email, user.Name, cancellationToken);
        }
        catch
        {
            // ignorar falha no envio do e-mail de boas-vindas
        }
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
