using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Models;

namespace UniversityMethodologicalDepartment.App.Contracts;

/// <summary>Поиск по всем справочникам через слой данных приложения (IDataService).</summary>
public interface IReferenceSearchService
{
    /// <param name="query">Строка запроса; пустая даёт пустой результат.</param>
    /// <param name="maxResults">Максимум элементов в ответе.</param>
    Task<IReadOnlyList<ReferenceSearchItem>> SearchAsync(
        string query,
        int maxResults,
        ReferenceSearchScope scope = ReferenceSearchScope.All,
        CancellationToken cancellationToken = default);
}
