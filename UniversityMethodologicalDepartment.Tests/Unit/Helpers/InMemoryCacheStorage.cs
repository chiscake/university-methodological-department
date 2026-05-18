using System.Collections.Concurrent;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// In-memory реализация <see cref="ICacheStorage"/> со счётчиками вызовов для проверок инвалидаций.
/// Подходит для юнит-тестов <see cref="UniversityMethodologicalDepartment.Bus.Services.CachedDataService"/>.
/// </summary>
internal sealed class InMemoryCacheStorage : ICacheStorage
{
    private readonly ConcurrentDictionary<string, string> _store = new(StringComparer.Ordinal);

    public int SetCount { get; private set; }
    public int RemoveCount { get; private set; }

    public IReadOnlyDictionary<string, string> Snapshot => _store;

    public bool TryGetValue(string key, out string? value)
    {
        if (_store.TryGetValue(key, out var stored))
        {
            value = stored;
            return true;
        }

        value = null;
        return false;
    }

    public void Set(string key, string value)
    {
        SetCount++;
        _store[key] = value;
    }

    public void Remove(string key)
    {
        RemoveCount++;
        _store.TryRemove(key, out _);
    }

    public void Seed(string key, string value) => _store[key] = value;

    public bool ContainsKey(string key) => _store.ContainsKey(key);
}
