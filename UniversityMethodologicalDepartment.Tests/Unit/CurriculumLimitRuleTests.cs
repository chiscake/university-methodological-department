using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;

namespace UniversityMethodologicalDepartment.Tests.Unit;

/// <summary>
/// Юнит-тесты <see cref="CurriculumLimitRule"/>: правило срабатывает строго по стабильному
/// маркеру в HINT и игнорирует прочие случаи.
/// </summary>
public sealed class CurriculumLimitRuleTests
{
    private static PostgresErrorInfo CreateInfo(string sqlState, string messageText, string? hint)
        => new(sqlState, messageText, hint, ConstraintName: null, ColumnName: null, TableName: null);

    [Fact]
    public void TryRecognize_WithMatchingHint_ReturnsCurriculumLimitError()
    {
        var rule = new CurriculumLimitRule();
        var info = CreateInfo("P0001", "Лимит 7 предметов превышен.", CurriculumLimitRule.HintMarker);

        var result = rule.TryRecognize(info);

        Assert.NotNull(result);
        Assert.Equal(DatabaseBusinessErrorCodes.CurriculumLimitExceeded, result!.Code);
        Assert.Equal("Лимит 7 предметов превышен.", result.Message);
        Assert.Same(info, result.Raw);
    }

    [Fact]
    public void TryRecognize_WithoutHint_ReturnsNull()
    {
        var rule = new CurriculumLimitRule();
        var info = CreateInfo("P0001", "Some other message", hint: null);

        Assert.Null(rule.TryRecognize(info));
    }

    [Fact]
    public void TryRecognize_WithDifferentHint_ReturnsNull()
    {
        var rule = new CurriculumLimitRule();
        var info = CreateInfo("P0001", "Some other message", hint: "OTHER_MARKER");

        Assert.Null(rule.TryRecognize(info));
    }

    [Fact]
    public void TryRecognize_HintComparisonIsCaseSensitive()
    {
        var rule = new CurriculumLimitRule();
        var info = CreateInfo("P0001", "msg", hint: "curriculum_limit_exceeded");

        Assert.Null(rule.TryRecognize(info));
    }
}
