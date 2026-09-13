namespace VistoriaApi.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken);
}
