using System;
using System.IO;
using System.Text;
using Helpers.Microsoft;
using Microsoft.Windows.Storage;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Реализация <see cref="ICacheStorage"/> через файлы в LocalFolder приложения.
/// Единственное хранилище кеша списков (факультеты, кафедры, сотрудники и т.д.); LocalSettings — только для настроек приложения.
/// </summary>
public sealed class FileBackedCacheStorage : ICacheStorage
{
    private const string CacheFolderName = "Cache";
    private readonly string _cacheFolderPath;
    private readonly object _sync = new();

    public FileBackedCacheStorage()
    {
        var localFolderPath = NativeMethods.IsAppPackaged
            ? ApplicationData.GetDefault().LocalFolder.Path
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ProcessInfoHelper.ProductName);

        _cacheFolderPath = Path.Combine(localFolderPath, CacheFolderName);
        Directory.CreateDirectory(_cacheFolderPath);
    }

    public bool TryGetValue(string key, out string? value)
    {
        var filePath = GetFilePath(key);
        lock (_sync)
        {
            if (!File.Exists(filePath))
            {
                value = null;
                return false;
            }

            value = File.ReadAllText(filePath, Encoding.UTF8);
            return true;
        }
    }

    public void Set(string key, string value)
    {
        var filePath = GetFilePath(key);
        lock (_sync)
        {
            File.WriteAllText(filePath, value ?? string.Empty, Encoding.UTF8);
        }
    }

    public void Remove(string key)
    {
        var filePath = GetFilePath(key);
        lock (_sync)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private string GetFilePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

        var fileName = SanitizeFileName(key) + ".json";
        return Path.Combine(_cacheFolderPath, fileName);
    }

    private static string SanitizeFileName(string key)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(key.Length);

        foreach (var ch in key)
            builder.Append(Array.IndexOf(invalidChars, ch) >= 0 ? '_' : ch);

        return builder.ToString();
    }
}
