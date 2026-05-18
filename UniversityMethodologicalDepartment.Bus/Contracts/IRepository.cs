using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Базовый репозиторий для сущностей: чтение списка и по Id, добавление, обновление и удаление.
/// Контекст передаётся вызывающим (фасад/сервис), сохранение выполняет вызывающий код.
/// </summary>
/// <typeparam name="T">Тип сущности (класс, зарегистрированный в <see cref="AppDbContext"/>).</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Возвращает все сущности типа <typeparamref name="T"/>.</summary>
    Task<List<T>> GetAllAsync(AppDbContext context, CancellationToken cancellationToken = default);

    /// <summary>Возвращает сущность по идентификатору или null.</summary>
    Task<T?> GetByIdAsync(AppDbContext context, int id, CancellationToken cancellationToken = default);

    /// <summary>Добавляет сущность в контекст (без вызова SaveChanges).</summary>
    Task AddAsync(AppDbContext context, T entity, CancellationToken cancellationToken = default);

    /// <summary>Помечает сущность как изменённую.</summary>
    void Update(AppDbContext context, T entity);

    /// <summary>Помечает сущность на удаление.</summary>
    void Delete(AppDbContext context, T entity);
}
