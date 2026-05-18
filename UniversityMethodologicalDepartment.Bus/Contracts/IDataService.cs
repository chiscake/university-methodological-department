using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Фасад доступа к данным: факультеты, кафедры, сотрудники, дисциплины, учебные планы и generic CRUD с учётом прав (RLS).
/// </summary>
public interface IDataService
{
    /// <summary>
    /// При true доступны создание, изменение и удаление (после авторизации); при false — только чтение (анонимный вход).
    /// </summary>
    /// <summary>
    /// При true доступны создание, изменение и удаление (после авторизации); при false — только чтение (анонимный вход).
    /// </summary>
    bool CanMutate { get; }

    /// <summary>
    /// Вызывается после успешного обновления кеша списка из БД (фоновое обновление или первая загрузка).
    /// Обработчики, которые обновляют UI, должны переходить на UI-поток.
    /// </summary>
    event EventHandler<CacheRefreshedEventArgs>? CacheRefreshed;

    /// <summary>
    /// Проверяет, доступен ли в локальном кеше непустой список для заданного набора сущностей.
    /// </summary>
    Task<bool> HasCachedDataAsync(EntitySet entitySet, CancellationToken cancellationToken = default);

    /// <summary>Возвращает список факультетов с деканами.</summary>
    Task<IReadOnlyList<Faculty>> GetFacultiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Возвращает список кафедр, опционально отфильтрованный по факультету.</summary>
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(int? facultyId = null, CancellationToken cancellationToken = default);

    /// <summary>Возвращает факультет по идентификатору с деканом и списком кафедр, или null.</summary>
    Task<Faculty?> GetFacultyWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Возвращает кафедру по идентификатору с факультетом, заведующим, сотрудниками, дисциплинами и секциями, или null.</summary>
    Task<Department?> GetDepartmentWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Возвращает секцию по идентификатору с кафедрой, руководителем и сотрудниками, или null.</summary>
    Task<Section?> GetSectionWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Возвращает дисциплину по идентификатору с кафедрой и элементами учебного плана, или null.</summary>
    Task<Discipline?> GetDisciplineWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Возвращает элемент учебного плана по идентификатору с дисциплиной и специальностью, или null.</summary>
    Task<CurriculumItem?> GetCurriculumItemWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Возвращает сотрудника по идентификатору с кафедрой и секцией, или null.</summary>
    Task<Employee?> GetEmployeeWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Возвращает список сотрудников, опционально отфильтрованный по кафедре.</summary>
    Task<IReadOnlyList<Employee>> GetEmployeesAsync(int? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>Возвращает список секций, опционально отфильтрованный по кафедре.</summary>
    Task<IReadOnlyList<Section>> GetSectionsAsync(int? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>Возвращает список специальностей.</summary>
    Task<IReadOnlyList<Specialty>> GetSpecialtiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Возвращает список дисциплин, опционально отфильтрованный по кафедре.</summary>
    Task<IReadOnlyList<Discipline>> GetDisciplinesAsync(int? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>Возвращает элементы учебного плана с опциональной фильтрацией по специальности и дисциплине.</summary>
    Task<IReadOnlyList<CurriculumItem>> GetCurriculumItemsAsync(
        int? specialtyId = null,
        int? disciplineId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Возвращает журнал аудита (только для администраторов), отсортированный по убыванию времени.</summary>
    Task<IReadOnlyList<AuditLog>> GetAuditLogAsync(int take = 200, CancellationToken cancellationToken = default);

    /// <summary>Возвращает все сущности указанного типа.</summary>
    Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>Возвращает сущность по идентификатору или null.</summary>
    Task<T?> GetByIdAsync<T>(int id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Добавляет сущность; возвращает false, если изменение запрещено.</summary>
    Task<bool> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Обновляет сущность; возвращает false, если изменение запрещено.</summary>
    Task<bool> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Удаляет сущность по идентификатору; возвращает false, если изменение запрещено или сущность не найдена.</summary>
    Task<bool> DeleteAsync<T>(int id, CancellationToken cancellationToken = default) where T : class;
}
