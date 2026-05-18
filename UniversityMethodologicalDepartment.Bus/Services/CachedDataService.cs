using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Bus.Services;

/// <summary>
/// Ключи кеша для списков сущностей (префикс по пользователю добавляется в рантайме).
/// </summary>
internal static class CacheKeys
{
    public const string Faculties = "Faculties";
    public const string Departments = "Departments";
    public const string Employees = "Employees";
    public const string Sections = "Sections";
    public const string Specialties = "Specialties";
    public const string Disciplines = "Disciplines";
    public const string CurriculumItems = "CurriculumItems";
}

/// <summary>
/// Декоратор <see cref="IDataService"/> с двухуровневым кешем (память + <see cref="ICacheStorage"/>).
/// При первом запросе загружает данные из хранилища в память (по ключам с префиксом текущего пользователя),
/// при наличии — возвращает из кеша и запускает фоновое обновление из БД; при отсутствии — запрос к inner,
/// сохранение в кеш и возврат. При успешных Add/Update/Delete инвалидирует соответствующий ключ кеша.
/// Кешируются списки: факультеты, кафедры, сотрудники, секции, специальности, дисциплины, элементы учебного плана.
/// </summary>
public sealed class CachedDataService : IDataService
{
    private readonly IDataService _inner;
    private readonly IAuthService _authService;
    private readonly ICacheStorage _storage;
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly Dictionary<string, object> _memory = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _loaded;

    /// <summary>Создаёт декоратор с заданным внутренним сервисом, сервисом аутентификации и хранилищем кеша.</summary>
    /// <param name="inner">Сервис доступа к БД (обычно <see cref="DataService"/>).</param>
    /// <param name="authService">Сервис аутентификации для префикса ключей кеша по пользователю.</param>
    /// <param name="storage">Хранилище кеша между сессиями (в App — LocalSettings, в тестах — in-memory).</param>
    public CachedDataService(IDataService inner, IAuthService authService, ICacheStorage storage)
    {
        _inner = inner;
        _authService = authService;
        _storage = storage;
        _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public bool CanMutate => _inner.CanMutate;

    public event EventHandler<CacheRefreshedEventArgs>? CacheRefreshed;

    public async Task<bool> HasCachedDataAsync(EntitySet entitySet, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var key = EntitySetToCacheKey(entitySet);

        lock (_memory)
        {
            return key switch
            {
                CacheKeys.Faculties => _memory.TryGetValue(key, out var faculties) && faculties is List<Faculty> f && f.Count > 0,
                CacheKeys.Departments => _memory.TryGetValue(key, out var departments) && departments is List<Department> d && d.Count > 0,
                CacheKeys.Employees => _memory.TryGetValue(key, out var employees) && employees is List<Employee> e && e.Count > 0,
                CacheKeys.Sections => _memory.TryGetValue(key, out var sections) && sections is List<Section> s && s.Count > 0,
                CacheKeys.Specialties => _memory.TryGetValue(key, out var specialties) && specialties is List<Specialty> sp && sp.Count > 0,
                CacheKeys.Disciplines => _memory.TryGetValue(key, out var disciplines) && disciplines is List<Discipline> di && di.Count > 0,
                CacheKeys.CurriculumItems => _memory.TryGetValue(key, out var curriculumItems) && curriculumItems is List<CurriculumItem> ci && ci.Count > 0,
                _ => false
            };
        }
    }

    private string GetCachePrefix()
    {
        var userId = _authService.CurrentUser?.Id ?? "anon";
        return "Cache." + userId + ".";
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
            return;
        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_loaded)
                return;
            var prefix = GetCachePrefix();
            var loadedFromDisk = 0;
            foreach (var key in new[] { CacheKeys.Faculties, CacheKeys.Departments, CacheKeys.Employees, CacheKeys.Sections, CacheKeys.Specialties, CacheKeys.Disciplines, CacheKeys.CurriculumItems })
            {
                var fullKey = prefix + key;
                if (_storage.TryGetValue(fullKey, out var json) && !string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        var data = DeserializeByKey(key, json);
                        if (data != null)
                        {
                            lock (_memory)
                                _memory[key] = data;
                            loadedFromDisk++;
                        }
                    }
                    catch (JsonException) { /* ignore corrupted cache */ }
                }
            }
            Debug.WriteLine($"[CachedDataService] EnsureLoaded: prefix={prefix}, keys loaded from storage={loadedFromDisk}.");
            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private object? DeserializeByKey(string key, string json)
    {
        return key switch
        {
            CacheKeys.Faculties => JsonSerializer.Deserialize<List<Faculty>>(json, _jsonOptions),
            CacheKeys.Departments => JsonSerializer.Deserialize<List<Department>>(json, _jsonOptions),
            CacheKeys.Employees => JsonSerializer.Deserialize<List<Employee>>(json, _jsonOptions),
            CacheKeys.Sections => JsonSerializer.Deserialize<List<Section>>(json, _jsonOptions),
            CacheKeys.Specialties => JsonSerializer.Deserialize<List<Specialty>>(json, _jsonOptions),
            CacheKeys.Disciplines => JsonSerializer.Deserialize<List<Discipline>>(json, _jsonOptions),
            CacheKeys.CurriculumItems => JsonSerializer.Deserialize<List<CurriculumItem>>(json, _jsonOptions),
            _ => null
        };
    }

    private void SaveToStorage(string key, object data)
    {
        var fullKey = GetCachePrefix() + key;
        var json = JsonSerializer.Serialize(data, data.GetType(), _jsonOptions);
        _storage.Set(fullKey, json);
    }

    private static string? EntityTypeToCacheKey(Type type)
    {
        if (type == typeof(Faculty)) return CacheKeys.Faculties;
        if (type == typeof(Department)) return CacheKeys.Departments;
        if (type == typeof(Employee)) return CacheKeys.Employees;
        if (type == typeof(Section)) return CacheKeys.Sections;
        if (type == typeof(Specialty)) return CacheKeys.Specialties;
        if (type == typeof(Discipline)) return CacheKeys.Disciplines;
        if (type == typeof(CurriculumItem)) return CacheKeys.CurriculumItems;
        return null;
    }

    private static string EntitySetToCacheKey(EntitySet entitySet)
    {
        return entitySet switch
        {
            EntitySet.Faculties => CacheKeys.Faculties,
            EntitySet.Departments => CacheKeys.Departments,
            EntitySet.Employees => CacheKeys.Employees,
            EntitySet.Sections => CacheKeys.Sections,
            EntitySet.Specialties => CacheKeys.Specialties,
            EntitySet.Disciplines => CacheKeys.Disciplines,
            EntitySet.CurriculumItems => CacheKeys.CurriculumItems,
            _ => throw new ArgumentOutOfRangeException(nameof(entitySet), entitySet, "Unsupported entity set.")
        };
    }

    private static EntitySet CacheKeyToEntitySet(string key)
    {
        return key switch
        {
            CacheKeys.Faculties => EntitySet.Faculties,
            CacheKeys.Departments => EntitySet.Departments,
            CacheKeys.Employees => EntitySet.Employees,
            CacheKeys.Sections => EntitySet.Sections,
            CacheKeys.Specialties => EntitySet.Specialties,
            CacheKeys.Disciplines => EntitySet.Disciplines,
            CacheKeys.CurriculumItems => EntitySet.CurriculumItems,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unsupported cache key.")
        };
    }

    private void RaiseCacheRefreshed(string cacheKey)
        => CacheRefreshed?.Invoke(this, new CacheRefreshedEventArgs(CacheKeyToEntitySet(cacheKey)));

    private void InvalidateKey(string key)
    {
        lock (_memory)
            _memory.Remove(key);
        var fullKey = GetCachePrefix() + key;
        _storage.Remove(fullKey);
    }

    public async Task<IReadOnlyList<Faculty>> GetFacultiesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (_memory)
        {
            if (_memory.TryGetValue(CacheKeys.Faculties, out var cached) && cached is List<Faculty> list && list.Count > 0)
            {
                Debug.WriteLine($"[CachedDataService] GetFaculties: returning from cache, count={list.Count}.");
                _ = RefreshFacultiesAsync();
                return list;
            }
        }
        Debug.WriteLine("[CachedDataService] GetFaculties: cache miss or empty, querying DB.");
        try
        {
            var fresh = await _inner.GetFacultiesAsync(cancellationToken).ConfigureAwait(false);
            var result = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.Faculties] = result;
            SaveToStorage(CacheKeys.Faculties, result);
            RaiseCacheRefreshed(CacheKeys.Faculties);
            Debug.WriteLine($"[CachedDataService] GetFaculties: DB returned count={result.Count}, saved to cache.");
            return result;
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            Debug.WriteLine($"[CachedDataService] GetFaculties: DB error, using fallback: {ex.Message} | Inner: {inner}");
            lock (_memory)
            {
                if (_memory.TryGetValue(CacheKeys.Faculties, out var fallback) && fallback is List<Faculty> list)
                {
                    Debug.WriteLine($"[CachedDataService] GetFaculties: returning cached fallback, count={list.Count}.");
                    return list;
                }
            }
            Debug.WriteLine("[CachedDataService] GetFaculties: no cache, returning empty list.");
            return new List<Faculty>();
        }
    }

    private async Task RefreshFacultiesAsync()
    {
        try
        {
            var fresh = await _inner.GetFacultiesAsync().ConfigureAwait(false);
            var list = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.Faculties] = list;
            SaveToStorage(CacheKeys.Faculties, list);
            RaiseCacheRefreshed(CacheKeys.Faculties);
            Debug.WriteLine($"[CachedDataService] RefreshFaculties: background refresh ok, count={list.Count}.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CachedDataService] RefreshFaculties: background refresh failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(int? facultyId = null, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (_memory)
        {
            if (_memory.TryGetValue(CacheKeys.Departments, out var cached) && cached is List<Department> list && list.Count > 0)
            {
                var returned = facultyId.HasValue ? list.Where(d => d.FacultyId == facultyId.Value).ToList() : list;
                Debug.WriteLine($"[CachedDataService] GetDepartments: returning from cache, count={returned.Count}, facultyId={facultyId}.");
                _ = RefreshDepartmentsAsync();
                return returned;
            }
        }
        Debug.WriteLine($"[CachedDataService] GetDepartments: cache miss or empty, querying DB, facultyId={facultyId}.");
        try
        {
            var fresh = await _inner.GetDepartmentsAsync(facultyId, cancellationToken).ConfigureAwait(false);
            var result = fresh.ToList();
            if (!facultyId.HasValue)
            {
                lock (_memory)
                    _memory[CacheKeys.Departments] = result;
                SaveToStorage(CacheKeys.Departments, result);
                RaiseCacheRefreshed(CacheKeys.Departments);
            }
            Debug.WriteLine($"[CachedDataService] GetDepartments: DB returned count={result.Count}, saved to cache.");
            return result;
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            Debug.WriteLine($"[CachedDataService] GetDepartments: DB error, using fallback: {ex.Message} | Inner: {inner}");
            lock (_memory)
            {
                if (_memory.TryGetValue(CacheKeys.Departments, out var fallback) && fallback is List<Department> list)
                {
                    var returned = facultyId.HasValue ? list.Where(d => d.FacultyId == facultyId.Value).ToList() : list;
                    Debug.WriteLine($"[CachedDataService] GetDepartments: returning cached fallback, count={returned.Count}.");
                    return returned;
                }
            }
            Debug.WriteLine("[CachedDataService] GetDepartments: no cache, returning empty list.");
            return new List<Department>();
        }
    }

    public Task<Faculty?> GetFacultyWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => _inner.GetFacultyWithDetailsAsync(id, cancellationToken);

    public Task<Department?> GetDepartmentWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => _inner.GetDepartmentWithDetailsAsync(id, cancellationToken);

    public Task<Section?> GetSectionWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => _inner.GetSectionWithDetailsAsync(id, cancellationToken);

    public Task<Discipline?> GetDisciplineWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => _inner.GetDisciplineWithDetailsAsync(id, cancellationToken);

    public Task<CurriculumItem?> GetCurriculumItemWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => _inner.GetCurriculumItemWithDetailsAsync(id, cancellationToken);

    public Task<Employee?> GetEmployeeWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => _inner.GetEmployeeWithDetailsAsync(id, cancellationToken);

    private async Task RefreshDepartmentsAsync()
    {
        try
        {
            var fresh = await _inner.GetDepartmentsAsync(null).ConfigureAwait(false);
            var list = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.Departments] = list;
            SaveToStorage(CacheKeys.Departments, list);
            RaiseCacheRefreshed(CacheKeys.Departments);
            Debug.WriteLine($"[CachedDataService] RefreshDepartments: background refresh ok, count={list.Count}.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CachedDataService] RefreshDepartments: background refresh failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (_memory)
        {
            if (_memory.TryGetValue(CacheKeys.Employees, out var cached) && cached is List<Employee> list && list.Count > 0)
            {
                _ = RefreshEmployeesAsync();
                if (departmentId.HasValue)
                    return list.Where(e => e.DepartmentId == departmentId.Value).ToList();
                return list;
            }
        }
        var fresh = await _inner.GetEmployeesAsync(departmentId, cancellationToken).ConfigureAwait(false);
        var result = fresh.ToList();
        if (!departmentId.HasValue)
        {
            lock (_memory)
                _memory[CacheKeys.Employees] = result;
            SaveToStorage(CacheKeys.Employees, result);
            RaiseCacheRefreshed(CacheKeys.Employees);
        }
        return result;
    }

    private async Task RefreshEmployeesAsync()
    {
        try
        {
            var fresh = await _inner.GetEmployeesAsync(null).ConfigureAwait(false);
            var list = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.Employees] = list;
            SaveToStorage(CacheKeys.Employees, list);
            RaiseCacheRefreshed(CacheKeys.Employees);
        }
        catch { }
    }

    public async Task<IReadOnlyList<Section>> GetSectionsAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (_memory)
        {
            if (_memory.TryGetValue(CacheKeys.Sections, out var cached) && cached is List<Section> list && list.Count > 0)
            {
                _ = RefreshSectionsAsync();
                if (departmentId.HasValue)
                    return list.Where(s => s.DepartmentId == departmentId.Value).ToList();
                return list;
            }
        }
        var fresh = await _inner.GetSectionsAsync(departmentId, cancellationToken).ConfigureAwait(false);
        var result = fresh.ToList();
        if (!departmentId.HasValue)
        {
            lock (_memory)
                _memory[CacheKeys.Sections] = result;
            SaveToStorage(CacheKeys.Sections, result);
            RaiseCacheRefreshed(CacheKeys.Sections);
        }
        return result;
    }

    private async Task RefreshSectionsAsync()
    {
        try
        {
            var fresh = await _inner.GetSectionsAsync(null).ConfigureAwait(false);
            var list = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.Sections] = list;
            SaveToStorage(CacheKeys.Sections, list);
            RaiseCacheRefreshed(CacheKeys.Sections);
        }
        catch { }
    }

    public async Task<IReadOnlyList<Specialty>> GetSpecialtiesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (_memory)
        {
            if (_memory.TryGetValue(CacheKeys.Specialties, out var cached) && cached is List<Specialty> list && list.Count > 0)
            {
                _ = RefreshSpecialtiesAsync();
                return list;
            }
        }
        var fresh = await _inner.GetSpecialtiesAsync(cancellationToken).ConfigureAwait(false);
        var result = fresh.ToList();
        lock (_memory)
            _memory[CacheKeys.Specialties] = result;
        SaveToStorage(CacheKeys.Specialties, result);
        RaiseCacheRefreshed(CacheKeys.Specialties);
        return result;
    }

    private async Task RefreshSpecialtiesAsync()
    {
        try
        {
            var fresh = await _inner.GetSpecialtiesAsync().ConfigureAwait(false);
            var list = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.Specialties] = list;
            SaveToStorage(CacheKeys.Specialties, list);
            RaiseCacheRefreshed(CacheKeys.Specialties);
        }
        catch { }
    }

    public async Task<IReadOnlyList<Discipline>> GetDisciplinesAsync(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (_memory)
        {
            if (_memory.TryGetValue(CacheKeys.Disciplines, out var cached) && cached is List<Discipline> list && list.Count > 0)
            {
                _ = RefreshDisciplinesAsync();
                if (departmentId.HasValue)
                    return list.Where(d => d.DepartmentId == departmentId.Value).ToList();
                return list;
            }
        }
        var fresh = await _inner.GetDisciplinesAsync(departmentId, cancellationToken).ConfigureAwait(false);
        var result = fresh.ToList();
        if (!departmentId.HasValue)
        {
            lock (_memory)
                _memory[CacheKeys.Disciplines] = result;
            SaveToStorage(CacheKeys.Disciplines, result);
            RaiseCacheRefreshed(CacheKeys.Disciplines);
        }
        return result;
    }

    private async Task RefreshDisciplinesAsync()
    {
        try
        {
            var fresh = await _inner.GetDisciplinesAsync(null).ConfigureAwait(false);
            var list = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.Disciplines] = list;
            SaveToStorage(CacheKeys.Disciplines, list);
            RaiseCacheRefreshed(CacheKeys.Disciplines);
        }
        catch { }
    }

    public async Task<IReadOnlyList<CurriculumItem>> GetCurriculumItemsAsync(
        int? specialtyId = null,
        int? disciplineId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (_memory)
        {
            if (_memory.TryGetValue(CacheKeys.CurriculumItems, out var cached) && cached is List<CurriculumItem> list && list.Count > 0)
            {
                _ = RefreshCurriculumItemsAsync();
                var query = list.AsEnumerable();
                if (specialtyId.HasValue)
                    query = query.Where(ci => ci.SpecialtyId == specialtyId.Value);
                if (disciplineId.HasValue)
                    query = query.Where(ci => ci.DisciplineId == disciplineId.Value);
                return query.ToList();
            }
        }
        var fresh = await _inner.GetCurriculumItemsAsync(specialtyId, disciplineId, cancellationToken).ConfigureAwait(false);
        var result = fresh.ToList();
        if (!specialtyId.HasValue && !disciplineId.HasValue)
        {
            lock (_memory)
                _memory[CacheKeys.CurriculumItems] = result;
            SaveToStorage(CacheKeys.CurriculumItems, result);
            RaiseCacheRefreshed(CacheKeys.CurriculumItems);
        }
        return result;
    }

    private async Task RefreshCurriculumItemsAsync()
    {
        try
        {
            var fresh = await _inner.GetCurriculumItemsAsync(null, null).ConfigureAwait(false);
            var list = fresh.ToList();
            lock (_memory)
                _memory[CacheKeys.CurriculumItems] = list;
            SaveToStorage(CacheKeys.CurriculumItems, list);
            RaiseCacheRefreshed(CacheKeys.CurriculumItems);
        }
        catch { }
    }

    public Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class
        => _inner.GetAllAsync<T>(cancellationToken);

    public Task<IReadOnlyList<AuditLog>> GetAuditLogAsync(int take = 200, CancellationToken cancellationToken = default)
        => _inner.GetAuditLogAsync(take, cancellationToken);

    public Task<T?> GetByIdAsync<T>(int id, CancellationToken cancellationToken = default) where T : class
        => _inner.GetByIdAsync<T>(id, cancellationToken);

    public async Task<bool> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        var ok = await _inner.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        if (ok && EntityTypeToCacheKey(typeof(T)) is { } key)
            InvalidateKey(key);
        return ok;
    }

    public async Task<bool> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        var ok = await _inner.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (ok && EntityTypeToCacheKey(typeof(T)) is { } key)
            InvalidateKey(key);
        return ok;
    }

    public async Task<bool> DeleteAsync<T>(int id, CancellationToken cancellationToken = default) where T : class
    {
        var ok = await _inner.DeleteAsync<T>(id, cancellationToken).ConfigureAwait(false);
        if (ok && EntityTypeToCacheKey(typeof(T)) is { } key)
            InvalidateKey(key);
        return ok;
    }
}
