namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Абстракция хранилища кеша списков (ключ — значение). В App реализуется через файлы (FileBackedCacheStorage);
/// в тестах — in-memory реализация для изоляции от платформы. LocalSettings используется только для настроек приложения.
/// </summary>
public interface ICacheStorage
{
    /// <summary>Пытается получить значение по ключу.</summary>
    /// <param name="key">Ключ (например, префикс пользователя + имя набора: Cache.anon.Faculties).</param>
    /// <param name="value">Значение при успехе; иначе null.</param>
    /// <returns>true, если ключ найден и значение прочитано.</returns>
    bool TryGetValue(string key, out string? value);

    /// <summary>Сохраняет значение по ключу (перезаписывает при наличии).</summary>
    /// <param name="key">Ключ.</param>
    /// <param name="value">Строка для сохранения (например, JSON списка сущностей).</param>
    void Set(string key, string value);

    /// <summary>Удаляет запись по ключу.</summary>
    /// <param name="key">Ключ.</param>
    void Remove(string key);
}
