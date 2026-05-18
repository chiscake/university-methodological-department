using System;
using Npgsql;

namespace UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;

/// <summary>
/// Универсальный fallback для plpgsql <c>RAISE EXCEPTION</c>: SQLSTATE <c>P0001</c>
/// (см. <see cref="PostgresErrorCodes.RaiseException"/>). Текст ошибки из БД считается
/// готовым к показу пользователю — это соответствует текущим триггерам в <c>003_triggers.sql</c>,
/// чьи сообщения сформулированы на русском языке.
/// </summary>
/// <remarks>
/// Должно регистрироваться ПОСЛЕ специализированных правил (например, <see cref="CurriculumLimitRule"/>),
/// чтобы те имели возможность распознать ошибку по более точным признакам (HINT и т.п.).
/// </remarks>
public sealed class PlpgsqlRaiseExceptionRule : IDatabaseBusinessRule
{
    public DatabaseBusinessError? TryRecognize(PostgresErrorInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        if (!string.Equals(info.SqlState, PostgresErrorCodes.RaiseException, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(info.MessageText))
        {
            return null;
        }

        return new DatabaseBusinessError(
            DatabaseBusinessErrorCodes.BusinessRuleViolation,
            info.MessageText,
            info);
    }
}
