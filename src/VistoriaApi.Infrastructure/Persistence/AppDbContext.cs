using Microsoft.EntityFrameworkCore;
using VistoriaApi.Application.Abstractions;
using VistoriaApi.Domain.Entities;

namespace VistoriaApi.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> UsersSet => Set<User>();

    public DbSet<InspectionTemplate> TemplatesSet => Set<InspectionTemplate>();

    public DbSet<Inspection> InspectionsSet => Set<Inspection>();

    IQueryable<User> IAppDbContext.Users => UsersSet;

    IQueryable<InspectionTemplate> IAppDbContext.Templates => TemplatesSet;

    IQueryable<Inspection> IAppDbContext.Inspections => InspectionsSet;

    void IAppDbContext.Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        Set<TEntity>().Add(entity);
    }
}
