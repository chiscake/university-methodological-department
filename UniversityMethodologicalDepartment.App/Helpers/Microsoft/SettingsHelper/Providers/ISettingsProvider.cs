using System.Diagnostics.CodeAnalysis;

namespace Helpers.Microsoft;

public interface ISettingsProvider
{
    bool Contains(string key);
    object? Get(string key);
    void Set(string key, object value);

    [return: MaybeNull]
    T Get<T>(string key);
    void Set<T>(string key, T value);

    /// <summary>
    /// Removes all stored settings (full reset to empty state).
    /// </summary>
    void Clear();
}
