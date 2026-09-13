namespace VistoriaApi.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
}
