using VistoriaApi.Domain.Entities;

namespace VistoriaApi.Application.Abstractions;

public interface IAppDbContext
{
    IQueryable<User> Users { get; }

    IQueryable<InspectionTemplate> Templates { get; }

    IQueryable<Inspection> Inspections { get; }

    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
