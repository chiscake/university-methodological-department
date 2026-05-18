using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// Тестовый двойник <see cref="IAppDbContextFactory"/>, бросающий исключение при попытке создать контекст.
/// Используется в тестах путей, в которых обращение к БД не должно происходить (security gate).
/// </summary>
internal sealed class ThrowingDbContextFactory : IAppDbContextFactory
{
    public int CreateCallCount { get; private set; }

    public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        CreateCallCount++;
        throw new InvalidOperationException("DB must not be accessed in this code path.");
    }
}
