namespace UniversityMethodologicalDepartment.App.Models;

/// <summary>
/// Параметр навигации на страницу деталей сущности (факультет или кафедра).
/// </summary>
/// <param name="Id">Идентификатор сущности.</param>
/// <param name="Name">Отображаемое название (для заголовка страницы).</param>
/// <param name="IsCreateMode">Флаг режима создания новой записи.</param>
public record EntityDetailParameter(int Id, string Name, bool IsCreateMode = false);
