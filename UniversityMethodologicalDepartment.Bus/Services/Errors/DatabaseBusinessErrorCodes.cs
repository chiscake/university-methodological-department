namespace UniversityMethodologicalDepartment.Bus.Services.Errors;

/// <summary>
/// Стабильные коды распознанных бизнес-ошибок БД. Используются ViewModel'ями для специальной обработки
/// и тестами. Значения константны и должны изменяться только осознанно.
/// </summary>
public static class DatabaseBusinessErrorCodes
{
    /// <summary>Превышен лимит элементов учебного плана в паре (specialty_id, semester).</summary>
    public const string CurriculumLimitExceeded = "CURRICULUM_LIMIT_EXCEEDED";

    /// <summary>Срабатывание plpgsql <c>RAISE EXCEPTION</c> без специализированного маркера; текст берётся из БД.</summary>
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
}
