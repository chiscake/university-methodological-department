using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniversityMethodologicalDepartment.App.Pages.Analytics;

namespace UniversityMethodologicalDepartment.App.Contracts;

public interface IQueryResultExportService
{
    Task<byte[]> ExportAsync(
        QueryScenario scenario,
        string queryTitle,
        IReadOnlyList<QueryResultRow> rows,
        QueryExportFormat format,
        CancellationToken cancellationToken = default);

    string GetDefaultFileName(QueryScenario scenario);

    string GetDefaultExtension(QueryExportFormat format);
}
