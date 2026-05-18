using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Microsoft.Windows.Storage;

namespace Helpers.Microsoft;

public partial class ApplicationDataSettingsProvider : ISettingsProvider
{
    private readonly ApplicationDataContainer container;

    public ApplicationDataSettingsProvider(ApplicationDataContainer container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public bool Contains(string key) => container.Values.ContainsKey(key);

    public object? Get(string key) => container.Values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, object value) => container.Values[key] = value;

    [return: MaybeNull]
    public T Get<T>(string key)
    {
        if (!container.Values.TryGetValue(key, out var value))
            return default!;

        if (value is T t)
            return t;

        if (value is string str && !IsSimpleType(typeof(T)))
        {
            try
            {
                var typeInfo = SettingsJsonContext.Default.GetTypeInfo(typeof(T))!;
                var obj = JsonSerializer.Deserialize(str, typeInfo);
                return obj is T parsed ? parsed : default!;
            }
            catch (Exception)
            {
                HandleCorruptedKey(key);
                return default!;
            }
        }

        return (T)Convert.ChangeType(value, typeof(T))!;
    }

    private void HandleCorruptedKey(string key)
    {
        try
        {
            container.Values.Remove(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove corrupted key '{key}': {ex}");
        }
    }

    public void Set<T>(string key, T value)
    {
        object? storedValue = IsSimpleType(typeof(T))
            ? value
            : JsonSerializer.Serialize(value, SettingsJsonContext.Default.GetTypeInfo(typeof(T))!);

        container.Values[key] = storedValue!;
    }

    public void Clear()
    {
        foreach (var key in container.Values.Keys.ToList())
        {
            container.Values.Remove(key);
        }
    }

    private static readonly HashSet<Type> ExtraSimpleTypes = new()
    {
        typeof(string),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Windows.Foundation.Point),
        typeof(Windows.Foundation.Size),
        typeof(Windows.Foundation.Rect)
    };

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || ExtraSimpleTypes.Contains(type);
    }
}
