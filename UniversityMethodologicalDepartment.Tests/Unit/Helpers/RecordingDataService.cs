using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// Фейк <see cref="IDataService"/> с настраиваемыми результатами и счётчиками вызовов.
/// Позволяет имитировать как успешные ответы, так и исключения для проверки fallback в кеширующем декораторе.
/// </summary>
internal sealed class RecordingDataService : IDataService
{
#pragma warning disable CS0067 // событие не используется самим фейком
    public event EventHandler<CacheRefreshedEventArgs>? CacheRefreshed;
#pragma warning restore CS0067

    public bool CanMutate { get; set; } = true;

    public Func<IReadOnlyList<Faculty>>? FacultiesProvider { get; set; }
    public Func<int?, IReadOnlyList<Department>>? DepartmentsProvider { get; set; }
    public Func<int?, IReadOnlyList<Employee>>? EmployeesProvider { get; set; }
    public Func<int?, IReadOnlyList<Section>>? SectionsProvider { get; set; }
    public Func<IReadOnlyList<Specialty>>? SpecialtiesProvider { get; set; }
    public Func<int?, IReadOnlyList<Discipline>>? DisciplinesProvider { get; set; }
    public Func<int?, int?, IReadOnlyList<CurriculumItem>>? CurriculumItemsProvider { get; set; }

    public bool AddResult { get; set; } = true;
    public bool UpdateResult { get; set; } = true;
    public bool DeleteResult { get; set; } = true;

    public int FacultiesCallCount { get; private set; }
    public int DepartmentsCallCount { get; private set; }
    public int EmployeesCallCount { get; private set; }
    public int SectionsCallCount { get; private set; }
    public int SpecialtiesCallCount { get; private set; }
    public int DisciplinesCallCount { get; private set; }
    public int CurriculumItemsCallCount { get; private set; }

    public int AddCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }

    public Task<bool> HasCachedDataAsync(EntitySet entitySet, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<Faculty>> GetFacultiesAsync(CancellationToken cancellationToken = default)
    {
        FacultiesCallCount++;
        var provider = FacultiesProvider ?? throw new InvalidOperationException("FacultiesProvider not configured.");
        return Task.FromResult(provider());
    }

    public Task<IReadOnlyList<Department>> GetDepartmentsAsync(int? facultyId = null, CancellationToken cancellationToken = default)
    {
        DepartmentsCallCount++;
        var provider = DepartmentsProvider ?? throw new InvalidOperationException("DepartmentsProvider not configured.");
        return Task.FromResult(provider(facultyId));
    }

    public Task<Faculty?> GetFacultyWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<Faculty?>(null);

    public Task<Department?> GetDepartmentWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<Department?>(null);

    public Task<Section?> GetSectionWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<Section?>(null);

    public Task<Discipline?> GetDisciplineWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<Discipline?>(null);

    public Task<CurriculumItem?> GetCurriculumItemWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<CurriculumItem?>(null);

    public Task<Employee?> GetEmployeeWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<Employee?>(null);

    public Task<IReadOnlyList<Employee>> GetEmployeesAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        EmployeesCallCount++;
        var provider = EmployeesProvider ?? throw new InvalidOperationException("EmployeesProvider not configured.");
        return Task.FromResult(provider(departmentId));
    }

    public Task<IReadOnlyList<Section>> GetSectionsAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        SectionsCallCount++;
        var provider = SectionsProvider ?? throw new InvalidOperationException("SectionsProvider not configured.");
        return Task.FromResult(provider(departmentId));
    }

    public Task<IReadOnlyList<Specialty>> GetSpecialtiesAsync(CancellationToken cancellationToken = default)
    {
        SpecialtiesCallCount++;
        var provider = SpecialtiesProvider ?? throw new InvalidOperationException("SpecialtiesProvider not configured.");
        return Task.FromResult(provider());
    }

    public Task<IReadOnlyList<Discipline>> GetDisciplinesAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        DisciplinesCallCount++;
        var provider = DisciplinesProvider ?? throw new InvalidOperationException("DisciplinesProvider not configured.");
        return Task.FromResult(provider(departmentId));
    }

    public Task<IReadOnlyList<CurriculumItem>> GetCurriculumItemsAsync(
        int? specialtyId = null,
        int? disciplineId = null,
        CancellationToken cancellationToken = default)
    {
        CurriculumItemsCallCount++;
        var provider = CurriculumItemsProvider ?? throw new InvalidOperationException("CurriculumItemsProvider not configured.");
        return Task.FromResult(provider(specialtyId, disciplineId));
    }

    public Task<IReadOnlyList<AuditLog>> GetAuditLogAsync(int take = 200, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AuditLog>>([]);

    public Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class
        => Task.FromResult<IReadOnlyList<T>>([]);

    public Task<T?> GetByIdAsync<T>(int id, CancellationToken cancellationToken = default) where T : class
        => Task.FromResult<T?>(null);

    public Task<bool> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        AddCallCount++;
        return Task.FromResult(AddResult);
    }

    public Task<bool> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        UpdateCallCount++;
        return Task.FromResult(UpdateResult);
    }

    public Task<bool> DeleteAsync<T>(int id, CancellationToken cancellationToken = default) where T : class
    {
        DeleteCallCount++;
        return Task.FromResult(DeleteResult);
    }
}
