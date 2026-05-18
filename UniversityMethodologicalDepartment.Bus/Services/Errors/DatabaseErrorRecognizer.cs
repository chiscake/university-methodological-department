using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace UniversityMethodologicalDepartment.Bus.Services.Errors;

/// <summary>
/// Реализация <see cref="IDatabaseErrorRecognizer"/> на основе упорядоченного списка правил.
/// Перебирает правила слева направо; первое совпадение возвращается. При отсутствии совпадений
/// возвращает <c>null</c>, оставляя место для общего fallback на стороне вызывающего кода.
/// </summary>
public sealed class DatabaseErrorRecognizer : IDatabaseErrorRecognizer
{
    private readonly IReadOnlyList<IDatabaseBusinessRule> _rules;

    /// <summary>Создаёт распознаватель с заданным набором правил.</summary>
    public DatabaseErrorRecognizer(IEnumerable<IDatabaseBusinessRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToList();
    }

    public DatabaseBusinessError? Recognize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg)
            {
                return Recognize(PostgresErrorInfo.FromException(pg));
            }
        }

        return null;
    }

    public DatabaseBusinessError? Recognize(PostgresErrorInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        foreach (var rule in _rules)
        {
            var recognized = rule.TryRecognize(info);
            if (recognized is not null)
            {
                return recognized;
            }
        }

        return null;
    }
}
