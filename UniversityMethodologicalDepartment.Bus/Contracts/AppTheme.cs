namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Тема оформления приложения: светлая, тёмная или по умолчанию (системная).
/// </summary>
public enum AppTheme
{
    /// <summary>Светлая тема.</summary>
    Light = 0,

    /// <summary>Тёмная тема.</summary>
    Dark = 1,

    /// <summary>Следовать системной теме.</summary>
    Default = 2
}
