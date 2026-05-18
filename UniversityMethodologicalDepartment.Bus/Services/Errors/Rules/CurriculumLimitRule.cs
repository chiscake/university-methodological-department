using System;

namespace UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;

/// <summary>
/// Распознаёт срабатывание триггера лимита элементов учебного плана по стабильному маркеру в HINT
/// (см. <c>tr_curriculum_item_semester_limit</c> в <c>003_triggers.sql</c>). Сообщение для пользователя
/// берётся из <see cref="PostgresErrorInfo.MessageText"/> (триггер уже формирует русский текст с числом лимита).
/// </summary>
public sealed class CurriculumLimitRule : IDatabaseBusinessRule
{
    /// <summary>Стабильный маркер, выставляемый триггером в <c>USING HINT = ...</c>.</summary>
    public const string HintMarker = "CURRICULUM_LIMIT_EXCEEDED";

    public DatabaseBusinessError? TryRecognize(PostgresErrorInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        if (!string.Equals(info.Hint, HintMarker, StringComparison.Ordinal))
        {
            return null;
        }

        return new DatabaseBusinessError(
            DatabaseBusinessErrorCodes.CurriculumLimitExceeded,
            info.MessageText,
            info);
    }
}
