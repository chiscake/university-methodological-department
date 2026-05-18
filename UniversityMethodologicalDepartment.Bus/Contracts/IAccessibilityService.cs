namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Сервис доступности: озвучивание сообщений для пользователей со вспомогательными технологиями.
/// </summary>
public interface IAccessibilityService
{
    /// <summary>Озвучивает сообщение через системные средства доступности.</summary>
    /// <param name="message">Текст для озвучивания.</param>
    void Announce(string message);
}
