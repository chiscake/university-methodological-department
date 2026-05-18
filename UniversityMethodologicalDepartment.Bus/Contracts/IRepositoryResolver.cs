namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Разрешение репозиториев по типу сущности без зависимости от контейнера DI в Bus.
/// Реализация в App передаёт вызов в <see cref="IServiceProvider"/>; в тестах можно подставлять мок или фабрику.
/// </summary>
public interface IRepositoryResolver
{
    /// <summary>Возвращает репозиторий для сущностей типа <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Тип сущности (класс, зарегистрированный в <see cref="AppDbContext"/>).</typeparam>
    /// <returns>Экземпляр <see cref="IRepository{T}"/>.</returns>
    IRepository<T> GetRepository<T>() where T : class;
}
