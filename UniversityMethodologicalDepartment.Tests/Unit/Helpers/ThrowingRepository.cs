using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// Тестовый репозиторий, бросающий заранее заготовленное исключение на операциях мутации
/// (<see cref="AddAsync"/>, <see cref="Update"/>, <see cref="Delete"/>). Используется для проверки
/// обработки исключений в <see cref="UniversityMethodologicalDepartment.Bus.Services.DataService"/>
/// без реального обращения к БД.
/// </summary>
internal sealed class ThrowingRepository<T> : IRepository<T>
    where T : class
{
    private readonly Exception _exceptionToThrow;
    private readonly T? _entityToReturnOnGet;

    public ThrowingRepository(Exception exceptionToThrow, T? entityToReturnOnGet = null)
    {
        _exceptionToThrow = exceptionToThrow;
        _entityToReturnOnGet = entityToReturnOnGet;
    }

    public Task<List<T>> GetAllAsync(AppDbContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<T>());

    public Task<T?> GetByIdAsync(AppDbContext context, int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_entityToReturnOnGet);

    public Task AddAsync(AppDbContext context, T entity, CancellationToken cancellationToken = default)
        => throw _exceptionToThrow;

    public void Update(AppDbContext context, T entity)
        => throw _exceptionToThrow;

    public void Delete(AppDbContext context, T entity)
        => throw _exceptionToThrow;
}

/// <summary>
/// Резолвер, всегда возвращающий тот же <see cref="ThrowingRepository{T}"/>.
/// </summary>
internal sealed class FixedRepositoryResolver<TEntity> : IRepositoryResolver
    where TEntity : class
{
    private readonly IRepository<TEntity> _repository;

    public FixedRepositoryResolver(IRepository<TEntity> repository) => _repository = repository;

    public IRepository<T> GetRepository<T>() where T : class
    {
        if (_repository is IRepository<T> typed)
        {
            return typed;
        }

        throw new InvalidOperationException(
            $"FixedRepositoryResolver configured only for {typeof(TEntity).Name}, requested {typeof(T).Name}.");
    }
}
