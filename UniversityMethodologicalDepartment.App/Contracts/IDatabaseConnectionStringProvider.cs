namespace UniversityMethodologicalDepartment.App.Contracts;

/// <summary>Эффективная строка подключения: пользовательский override из Credential Manager, иначе <c>ConnectionStrings:DefaultConnection</c>.</summary>
public interface IDatabaseConnectionStringProvider
{
    /// <summary>True, если в хранилище есть сохранённый override.</summary>
    bool HasUserOverride { get; }

    /// <summary>Строка из конфигурации (без override), для подсказок в UI.</summary>
    string? GetConfigurationConnectionString();

    /// <summary>Override из Credential Manager, без fallback на конфиг; null если нет.</summary>
    string? GetUserOverrideConnectionString();

    /// <summary>Override при наличии, иначе строка из конфигурации. Бросает, если обе отсутствуют.</summary>
    string GetEffectiveConnectionString();

    /// <summary>Сохраняет override в Credential Manager (перезаписывает существующий).</summary>
    bool SetUserOverride(string connectionString);

    /// <summary>Удаляет сохранённый override.</summary>
    bool ClearUserOverride();
}
