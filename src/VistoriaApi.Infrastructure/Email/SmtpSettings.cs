namespace VistoriaApi.Infrastructure.Email;

public sealed class SmtpSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool UseSsl { get; init; }
    public string From { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }
}
