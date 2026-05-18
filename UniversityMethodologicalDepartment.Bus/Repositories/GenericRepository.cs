using Microsoft.EntityFrameworkCore;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Bus.Repositories;

/// <summary>
/// Реализация <see cref="IRepository{T}"/> через <see cref="DbContext.Set{T}"/>: чтение с AsNoTracking,
/// FindAsync по Id, Add/Update/Remove. Не вызывает SaveChanges — это делает фасад (например, <see cref="IDataService"/>).
/// </summary>
/// <typeparam name="T">Тип сущности.</typeparam>
public sealed class GenericRepository<T> : IRepository<T> where T : class
{
    public Task<List<T>> GetAllAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        return context.Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<T?> GetByIdAsync(AppDbContext context, int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        return entity;
    }

    public Task AddAsync(AppDbContext context, T entity, CancellationToken cancellationToken = default)
    {
        return context.Set<T>().AddAsync(entity, cancellationToken).AsTask();
    }

    public void Update(AppDbContext context, T entity)
    {
        context.Set<T>().Update(entity);
    }

    public void Delete(AppDbContext context, T entity)
    {
        context.Set<T>().Remove(entity);
    }
}
