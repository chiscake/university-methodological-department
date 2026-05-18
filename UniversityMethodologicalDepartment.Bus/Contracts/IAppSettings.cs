namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Настройки приложения: ориентация навигации и сброс к значениям по умолчанию.
/// </summary>
public interface IAppSettings
{
    /// <summary>Режим навигации «слева» (true) или «справа» (false).</summary>
    bool IsLeftMode { get; set; }

    /// <summary>Размер страницы для списков справочников (по умолчанию 50).</summary>
    int ListPageSize { get; set; }

    /// <summary>Сбрасывает настройки к значениям по умолчанию.</summary>
    void ResetToDefaults();
}
