namespace UniversityMethodologicalDepartment.Bus.Services.Errors;

/// <summary>
/// Правило распознавания одного класса бизнес-ошибок БД.
/// Реализации должны быть без состояния (stateless) и потокобезопасными.
/// </summary>
public interface IDatabaseBusinessRule
{
    /// <summary>
    /// Пытается распознать ошибку по полям <see cref="PostgresErrorInfo"/>.
    /// Возвращает <c>null</c>, если правило не применимо.
    /// </summary>
    DatabaseBusinessError? TryRecognize(PostgresErrorInfo info);
}
