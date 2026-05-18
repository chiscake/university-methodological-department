using Npgsql;

namespace UniversityMethodologicalDepartment.Bus.Services.Errors;

/// <summary>
/// Универсальное представление PostgreSQL-ошибки, не зависящее от типа <see cref="PostgresException"/>.
/// Используется правилами распознавания и юнит-тестами без необходимости конструировать <see cref="PostgresException"/>.
/// </summary>
/// <param name="SqlState">SQLSTATE код (например, <c>P0001</c> для <c>raise_exception</c>).</param>
/// <param name="MessageText">Основной текст ошибки из БД (без префикса).</param>
/// <param name="Hint">Содержимое поля HINT, если задано через <c>RAISE ... USING HINT = ...</c>.</param>
/// <param name="ConstraintName">Имя нарушенного ограничения (для FK/UNIQUE/CHECK), если доступно.</param>
/// <param name="ColumnName">Имя столбца, связанного с ошибкой, если доступно.</param>
/// <param name="TableName">Имя таблицы, связанной с ошибкой, если доступно.</param>
public sealed record PostgresErrorInfo(
    string SqlState,
    string MessageText,
    string? Hint,
    string? ConstraintName,
    string? ColumnName,
    string? TableName)
{
    /// <summary>Создаёт <see cref="PostgresErrorInfo"/> из реального <see cref="PostgresException"/>.</summary>
    public static PostgresErrorInfo FromException(PostgresException exception)
    {
        return new PostgresErrorInfo(
            exception.SqlState ?? string.Empty,
            exception.MessageText ?? string.Empty,
            string.IsNullOrEmpty(exception.Hint) ? null : exception.Hint,
            string.IsNullOrEmpty(exception.ConstraintName) ? null : exception.ConstraintName,
            string.IsNullOrEmpty(exception.ColumnName) ? null : exception.ColumnName,
            string.IsNullOrEmpty(exception.TableName) ? null : exception.TableName);
    }
}
