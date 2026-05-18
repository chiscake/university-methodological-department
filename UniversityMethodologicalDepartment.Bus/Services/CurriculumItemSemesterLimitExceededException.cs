using System;
using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;

namespace UniversityMethodologicalDepartment.Bus.Services;

/// <summary>
/// Бизнес-ошибка: попытка добавить или перенести запись <c>curriculum_item</c> в пару
/// (<c>specialty_id</c>, <c>semester</c>), где уже достигнут лимит в 7 предметов.
/// Распознаётся в <see cref="DataService"/> через <see cref="IDatabaseErrorRecognizer"/>
/// (правило <see cref="CurriculumLimitRule"/>) по стабильному маркеру PostgreSQL HINT
/// <see cref="PostgresHintMarker"/>; пользовательский русский текст приходит из БД.
/// </summary>
/// <remarks>
/// Специализированный подкласс <see cref="DatabaseBusinessException"/> для обратной совместимости
/// с существующими <c>catch</c>-блоками во ViewModel'ях.
/// </remarks>
public sealed class CurriculumItemSemesterLimitExceededException : DatabaseBusinessException
{
    /// <summary>
    /// Стабильный маркер ошибки в поле HINT исключения PostgreSQL,
    /// по которому слой данных распознаёт срабатывание триггера лимита.
    /// </summary>
    public const string PostgresHintMarker = CurriculumLimitRule.HintMarker;

    public CurriculumItemSemesterLimitExceededException(string message, Exception? innerException = null)
        : base(new DatabaseBusinessError(DatabaseBusinessErrorCodes.CurriculumLimitExceeded, message, null), innerException)
    {
    }

    internal CurriculumItemSemesterLimitExceededException(DatabaseBusinessError businessError, Exception? innerException = null)
        : base(businessError, innerException)
    {
    }
}
