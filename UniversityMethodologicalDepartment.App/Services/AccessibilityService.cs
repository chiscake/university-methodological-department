using Helpers.Microsoft;
using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Сервис доступности: озвучивание сообщений для пользователей со вспомогательными технологиями (экранные дикторы).
/// </summary>
public sealed class AccessibilityService : IAccessibilityService
{
    /// <summary>
    /// Озвучивает сообщение через системные средства доступности.
    /// </summary>
    /// <param name="message">Текст для озвучивания.</param>
    public void Announce(string message)
    {
        if (App.MainWindow.Content is not UIElement element)
        {
            return;
        }

        UIHelper.AnnounceActionForAccessibility(
            element,
            message,
            "ThemeChangedNotificationActivityId");
    }
}
