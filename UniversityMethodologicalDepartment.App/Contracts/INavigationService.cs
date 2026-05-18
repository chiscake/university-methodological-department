using System;

namespace UniversityMethodologicalDepartment.App.Contracts;

/// <summary>
/// Сервис навигации между страницами приложения (обёртка над Frame).
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Переход на страницу указанного типа.
    /// </summary>
    /// <param name="pageType">Тип страницы (должен наследовать <see cref="Microsoft.UI.Xaml.Controls.Page"/>).</param>
    /// <param name="parameter">Необязательный параметр, передаётся в целевую страницу.</param>
    void Navigate(Type pageType, object? parameter = null);

    /// <summary>
    /// Переход на предыдущую страницу в журнале навигации.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Возможен ли переход назад.
    /// </summary>
    bool CanGoBack { get; }
}
