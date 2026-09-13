using Microsoft.Extensions.Configuration;
using VistoriaApi.Application.Abstractions;

namespace VistoriaApi.Infrastructure.Files;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly IConfiguration _configuration;

    public LocalFileStorage(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var extension = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException("Tipo de arquivo não suportado.")
        };

        var configuredRoot = _configuration["Storage:Root"] ?? "uploads";
        var storageRoot = Path.GetFullPath(configuredRoot);
        var relativePath = Path.Combine(
            DateTime.UtcNow.ToString("yyyy"),
            DateTime.UtcNow.ToString("MM"),
            $"{Guid.NewGuid():N}{extension}");
        var fullPath = Path.Combine(storageRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = File.Create(fullPath);
        await stream.CopyToAsync(output, cancellationToken);

        return relativePath.Replace('\\', '/');
    }
}
