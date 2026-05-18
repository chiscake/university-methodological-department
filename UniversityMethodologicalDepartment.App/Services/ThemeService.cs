using Helpers.Microsoft;
using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Сервис темы оформления приложения: получение и установка светлой/тёмной/системной темы с синхронизацией заголовка окна.
/// </summary>
public sealed class ThemeService : IThemeService
{
    /// <summary>Возвращает текущую выбранную тему (Light, Dark или Default).</summary>
    public AppTheme GetTheme()
    {
        return ThemeHelper.RootTheme switch
        {
            ElementTheme.Light => AppTheme.Light,
            ElementTheme.Dark => AppTheme.Dark,
            _ => AppTheme.Default
        };
    }

    /// <summary>Устанавливает тему и применяет её к заголовку окна.</summary>
    /// <param name="theme">Тема: Light, Dark или Default (системная).</param>
    public void SetTheme(AppTheme theme)
    {
        var elementTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        ThemeHelper.RootTheme = elementTheme;
        var resolvedTheme = ThemeHelper.RootTheme == ElementTheme.Default ? ThemeHelper.ActualTheme : ThemeHelper.RootTheme;
        TitleBarHelper.ApplySystemThemeToCaptionButtons(App.MainWindow, resolvedTheme);
    }
}
