namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Сервис темы оформления: получение и установка светлой/тёмной/системной темы.
/// </summary>
public interface IThemeService
{
    /// <summary>Возвращает текущую выбранную тему.</summary>
    AppTheme GetTheme();

    /// <summary>Устанавливает тему оформления.</summary>
    void SetTheme(AppTheme theme);
}
