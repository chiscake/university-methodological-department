using UniversityMethodologicalDepartment.Bus.Models;

namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

public interface IChartDataService
{
    Task<IReadOnlyList<LabHoursByDepartmentPoint>> GetLabHoursByDepartmentAsync(
        int semester,
        CancellationToken cancellationToken = default);
}
