using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Services.Errors;

namespace UniversityMethodologicalDepartment.Bus.Services;

/// <summary>
/// Сервис доступа к данным: чтение и изменение сущностей (факультеты, кафедры, сотрудники, дисциплины и т.д.)
/// с учётом прав аутентифицированного пользователя (RLS через фабрику контекста и репозитории через резолвер).
/// </summary>
public sealed class DataService : IDataService
{
    private readonly IAppDbContextFactory _dbContextFactory;
    private readonly IRepositoryResolver _repositoryResolver;
    private readonly IAuthService _authService;
    private readonly IDatabaseErrorRecognizer _errorRecognizer;

    /// <summary>Создаёт сервис с заданной фабрикой контекста, резолвером репозиториев, сервисом аутентификации и распознавателем ошибок БД.</summary>
    /// <param name="dbContextFactory">Фабрика контекста БД (учёт пользователя и RLS).</param>
    /// <param name="repositoryResolver">Резолвер репозиториев по типу сущности.</param>
    /// <param name="authService">Сервис аутентификации (CanMutate, текущий пользователь).</param>
    /// <param name="errorRecognizer">Распознаватель бизнес-ошибок БД (правила HINT/SQLSTATE).</param>
    public DataService(
        IAppDbContextFactory dbContextFactory,
        IRepositoryResolver repositoryResolver,
        IAuthService authService,
        IDatabaseErrorRecognizer errorRecognizer)
    {
        _dbContextFactory = dbContextFactory;
        _repositoryResolver = repositoryResolver;
        _authService = authService;
        _errorRecognizer = errorRecognizer;
    }

    /// <summary>Разрешено ли изменять данные (добавление/обновление/удаление); true только для аутентифицированных пользователей.</summary>
    public bool CanMutate => _authService.IsAuthenticated;

    public event EventHandler<CacheRefreshedEventArgs>? CacheRefreshed
    {
        add { }
        remove { }
    }

    public Task<bool> HasCachedDataAsync(EntitySet entitySet, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    /// <summary>Возвращает список факультетов с деканами.</summary>
    public async Task<IReadOnlyList<Faculty>> GetFacultiesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Faculties
            .AsNoTracking()
            .Include(f => f.Dean)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Возвращает список кафедр, опционально отфильтрованный по факультету.</summary>
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(int? facultyId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Departments
            .AsNoTracking()
            .Include(d => d.Faculty)
            .Include(d => d.Head)
            .AsQueryable();

        if (facultyId.HasValue)
        {
            query = query.Where(d => d.FacultyId == facultyId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>Возвращает факультет по идентификатору с деканом и списком кафедр, или null.</summary>
    public async Task<Faculty?> GetFacultyWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Faculties
            .AsNoTracking()
            .Include(f => f.Dean)
            .Include(f => f.Departments)
            .Where(f => f.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Возвращает кафедру по идентификатору с факультетом, заведующим, сотрудниками, дисциплинами и секциями, или null.</summary>
    public async Task<Department?> GetDepartmentWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Departments
            .AsNoTracking()
            .Include(d => d.Faculty)
            .Include(d => d.Head)
            .Include(d => d.Employees)
            .Include(d => d.Disciplines)
            .Include(d => d.Sections)
            .Where(d => d.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Возвращает секцию по идентификатору с кафедрой, руководителем и сотрудниками, или null.</summary>
    public async Task<Section?> GetSectionWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Sections
            .AsNoTracking()
            .Include(s => s.Department)
            .Include(s => s.Head)
            .Include(s => s.Employees)
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Возвращает дисциплину по идентификатору с кафедрой и элементами учебного плана (со специальностью), или null.</summary>
    public async Task<Discipline?> GetDisciplineWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Disciplines
            .AsNoTracking()
            .Include(d => d.Department)
            .Include(d => d.CurriculumItems)
            .ThenInclude(ci => ci.Specialty)
            .Where(d => d.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Возвращает элемент учебного плана по идентификатору с дисциплиной и специальностью, или null.</summary>
    public async Task<CurriculumItem?> GetCurriculumItemWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CurriculumItems
            .AsNoTracking()
            .Include(ci => ci.Discipline)
            .Include(ci => ci.Specialty)
            .Where(ci => ci.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Возвращает сотрудника по идентификатору с кафедрой и секцией, или null.</summary>
    public async Task<Employee?> GetEmployeeWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Section)
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Возвращает список сотрудников, опционально отфильтрованный по кафедре.</summary>
    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>Возвращает список секций, опционально отфильтрованный по кафедре.</summary>
    public async Task<IReadOnlyList<Section>> GetSectionsAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Sections
            .AsNoTracking()
            .Include(s => s.Department)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(s => s.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>Возвращает список специальностей.</summary>
    public async Task<IReadOnlyList<Specialty>> GetSpecialtiesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var repository = _repositoryResolver.GetRepository<Specialty>();
        return await repository.GetAllAsync(db, cancellationToken);
    }

    /// <summary>Возвращает список дисциплин, опционально отфильтрованный по кафедре.</summary>
    public async Task<IReadOnlyList<Discipline>> GetDisciplinesAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Disciplines
            .AsNoTracking()
            .Include(d => d.Department)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(d => d.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>Возвращает элементы учебного плана с опциональной фильтрацией по специальности и дисциплине.</summary>
    public async Task<IReadOnlyList<CurriculumItem>> GetCurriculumItemsAsync(
        int? specialtyId = null,
        int? disciplineId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.CurriculumItems
            .AsNoTracking()
            .Include(ci => ci.Specialty)
            .Include(ci => ci.Discipline)
            .AsQueryable();

        if (specialtyId.HasValue)
        {
            query = query.Where(ci => ci.SpecialtyId == specialtyId.Value);
        }

        if (disciplineId.HasValue)
        {
            query = query.Where(ci => ci.DisciplineId == disciplineId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>Возвращает журнал аудита (только для администраторов), отсортированный по убыванию времени.</summary>
    public async Task<IReadOnlyList<AuditLog>> GetAuditLogAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        if (!_authService.IsAdmin)
        {
            return [];
        }

        try
        {
            var normalizedTake = Math.Clamp(take, 1, 1000);
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await db.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.OccurredAt)
                .Take(normalizedTake)
                .ToListAsync(cancellationToken);
        }
        catch (PostgresException ex)
        {
            Debug.WriteLine($"[DataService] GetAuditLogAsync postgres error {ex.SqlState}: {ex.MessageText}");
            if (string.Equals(ex.SqlState, PostgresErrorCodes.UndefinedColumn, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(ex.ColumnName))
            {
                Debug.WriteLine($"[DataService] Missing DB column for audit log query: {ex.ColumnName}. Check applied migrations.");
            }

            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DataService] GetAuditLogAsync unexpected error: {ex}");
            throw;
        }
    }

    /// <summary>Возвращает все сущности указанного типа.</summary>
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var repository = _repositoryResolver.GetRepository<T>();
        return await repository.GetAllAsync(db, cancellationToken);
    }

    /// <summary>Возвращает сущность по идентификатору или null.</summary>
    public async Task<T?> GetByIdAsync<T>(int id, CancellationToken cancellationToken = default) where T : class
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var repository = _repositoryResolver.GetRepository<T>();
        return await repository.GetByIdAsync(db, id, cancellationToken);
    }

    /// <summary>Добавляет сущность; возвращает false, если изменение запрещено (пользователь не аутентифицирован).</summary>
    /// <exception cref="DatabaseBusinessException">
    /// Распознанная бизнес-ошибка БД (см. <see cref="IDatabaseErrorRecognizer"/>); специализированный
    /// подкласс <see cref="CurriculumItemSemesterLimitExceededException"/> используется для лимита учебного плана.
    /// </exception>
    public async Task<bool> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        if (!CanMutate)
        {
            return false;
        }

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var repository = _repositoryResolver.GetRepository<T>();
            await repository.AddAsync(db, entity, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Debug.WriteLine($"[DataService] AddAsync concurrency issue for {typeof(T).Name}: {ex.Message}");
            return false;
        }
        catch (DbUpdateException ex)
        {
            return HandleDbUpdateException<T>(ex, nameof(AddAsync));
        }
    }

    /// <summary>Обновляет сущность; возвращает false, если изменение запрещено.</summary>
    /// <exception cref="DatabaseBusinessException">
    /// Распознанная бизнес-ошибка БД (см. <see cref="IDatabaseErrorRecognizer"/>); специализированный
    /// подкласс <see cref="CurriculumItemSemesterLimitExceededException"/> используется для лимита учебного плана.
    /// </exception>
    public async Task<bool> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        if (!CanMutate)
        {
            return false;
        }

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var repository = _repositoryResolver.GetRepository<T>();
            repository.Update(db, entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Debug.WriteLine($"[DataService] UpdateAsync concurrency issue for {typeof(T).Name}: {ex.Message}");
            return false;
        }
        catch (DbUpdateException ex)
        {
            return HandleDbUpdateException<T>(ex, nameof(UpdateAsync));
        }
    }

    /// <summary>
    /// Общая обработка <see cref="DbUpdateException"/>: распознаёт бизнес-ошибку через
    /// <see cref="IDatabaseErrorRecognizer"/> и пробрасывает <see cref="DatabaseBusinessException"/>;
    /// нераспознанные ошибки конвертируются в <c>false</c> (общий fallback).
    /// </summary>
    /// <returns>Всегда <c>false</c>, если выбрасывание исключения не требуется.</returns>
    private bool HandleDbUpdateException<T>(DbUpdateException exception, string operationName)
    {
        var businessError = _errorRecognizer.Recognize(exception);
        if (businessError is null)
        {
            Debug.WriteLine($"[DataService] {operationName} db update issue for {typeof(T).Name}: {exception.Message}");
            return false;
        }

        Debug.WriteLine($"[DataService] {operationName} business error {businessError.Code} for {typeof(T).Name}: {businessError.Message}");
        throw CreateBusinessException(businessError, exception);
    }

    private static DatabaseBusinessException CreateBusinessException(DatabaseBusinessError businessError, Exception inner)
    {
        return businessError.Code == DatabaseBusinessErrorCodes.CurriculumLimitExceeded
            ? new CurriculumItemSemesterLimitExceededException(businessError, inner)
            : new DatabaseBusinessException(businessError, inner);
    }

    /// <summary>Удаляет сущность по идентификатору; возвращает false, если изменение запрещено или сущность не найдена.</summary>
    public async Task<bool> DeleteAsync<T>(int id, CancellationToken cancellationToken = default) where T : class
    {
        if (!CanMutate)
        {
            return false;
        }

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var repository = _repositoryResolver.GetRepository<T>();
            var entity = await repository.GetByIdAsync(db, id, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            repository.Delete(db, entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Debug.WriteLine($"[DataService] DeleteAsync concurrency issue for {typeof(T).Name}: {ex.Message}");
            return false;
        }
        catch (DbUpdateException ex)
        {
            return HandleDbUpdateException<T>(ex, nameof(DeleteAsync));
        }
    }
}
