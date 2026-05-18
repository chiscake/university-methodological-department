using Helpers.Microsoft;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Сервис настроек приложения: ориентация навигации и сброс к значениям по умолчанию.
/// </summary>
public sealed class AppSettingsService : IAppSettings
{
    private const int DefaultListPageSize = 50;

    /// <summary>Режим навигации «слева» (true) или «справа» (false).</summary>
    public bool IsLeftMode
    {
        get => SettingsHelper.Current.IsLeftMode;
        set => NavigationOrientationHelper.IsLeftModeForElement(value);
    }

    /// <summary>Размер страницы для списков справочников.</summary>
    public int ListPageSize
    {
        get => SettingsHelper.Current.ListPageSize;
        set => SettingsHelper.Current.ListPageSize = value;
    }

    /// <summary>Сбрасывает настройки приложения к значениям по умолчанию.</summary>
    public void ResetToDefaults()
    {
        SettingsHelper.Current.ResetToDefaults();
        SettingsHelper.Current.ListPageSize = DefaultListPageSize;
    }
}
