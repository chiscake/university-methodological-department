using System.Threading;
using System.Threading.Tasks;

namespace UniversityMethodologicalDepartment.App.Contracts;

/// <summary>Проверка доступности БД по строке подключения без сохранения в Credential Manager.</summary>
public interface IDatabaseConnectionTester
{
    Task<(bool Success, string? ErrorMessage)> TestAsync(string connectionString, CancellationToken cancellationToken = default);
}
