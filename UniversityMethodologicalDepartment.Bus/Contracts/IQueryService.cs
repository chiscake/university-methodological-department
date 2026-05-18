using UniversityMethodologicalDepartment.Bus.Models;

namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

public interface IQueryService
{
    Task<IReadOnlyList<MultiDepartmentDisciplineRow>> GetMultiDepartmentDisciplinesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MultiSemesterDisciplineRow>> GetMultiSemesterDisciplinesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDisciplineCountRow>> GetDepartmentDisciplineCountsAsync(
        bool sortByCountDescending = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LectureLabDifferenceRow>> GetLectureLabDifferencesAsync(
        int departmentId,
        int semester,
        CancellationToken cancellationToken = default);
}
