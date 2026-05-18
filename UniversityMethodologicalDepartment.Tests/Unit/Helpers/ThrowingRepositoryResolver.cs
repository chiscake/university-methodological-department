using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// Тестовый двойник <see cref="IRepositoryResolver"/>, бросающий при попытке получить репозиторий.
/// Используется в путях, в которых не должно быть обращения ни к БД, ни к репозиториям.
/// </summary>
internal sealed class ThrowingRepositoryResolver : IRepositoryResolver
{
    public IRepository<T> GetRepository<T>() where T : class
        => throw new InvalidOperationException("Repository must not be resolved in this code path.");
}
