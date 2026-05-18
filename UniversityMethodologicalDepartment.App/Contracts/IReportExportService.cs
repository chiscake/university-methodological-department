using System.Threading;
using System.Threading.Tasks;
using UniversityMethodologicalDepartment.App.Models;

namespace UniversityMethodologicalDepartment.App.Contracts;

public interface IReportExportService
{
    Task<byte[]> GenerateAsync(ReportExportKind kind, int? specialtyId = null, CancellationToken cancellationToken = default);

    Task<ReportPreviewDocument> GetPreviewAsync(ReportExportKind kind, CancellationToken cancellationToken = default);

    string GetDefaultFileName(ReportExportKind kind);

    string GetDefaultExtension(ReportExportKind kind);
}
