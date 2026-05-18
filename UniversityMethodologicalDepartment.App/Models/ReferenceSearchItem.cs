namespace UniversityMethodologicalDepartment.App.Models;

/// <summary>Элемент результата глобального поиска по справочникам.</summary>
public sealed class ReferenceSearchItem
{
    public required ReferenceSearchKind Kind { get; init; }

    public required int Id { get; init; }

    /// <summary>Основная строка (название, ФИО).</summary>
    public required string Title { get; init; }

    /// <summary>Вторая строка в подсказке: тип и контекст.</summary>
    public string Subtitle { get; init; } = string.Empty;

    /// <summary>
    /// Возвращает отображаемое название для элементов, когда AutoSuggestBox
    /// использует ToString() (например, при выборе без TextMemberPath).
    /// </summary>
    public override string ToString() => Title;
}
