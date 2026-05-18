using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;

namespace UniversityMethodologicalDepartment.Tests.Unit;

/// <summary>
/// Юнит-тесты <see cref="PlpgsqlRaiseExceptionRule"/>: универсальный fallback для plpgsql RAISE EXCEPTION
/// (SQLSTATE P0001). Прочие SQLSTATE (FK, UNIQUE и т.п.) не должны распознаваться этим правилом.
/// </summary>
public sealed class PlpgsqlRaiseExceptionRuleTests
{
    private static PostgresErrorInfo CreateInfo(string sqlState, string messageText, string? hint = null)
        => new(sqlState, messageText, hint, ConstraintName: null, ColumnName: null, TableName: null);

    [Fact]
    public void TryRecognize_WithRaiseExceptionState_ReturnsBusinessRuleViolation()
    {
        var rule = new PlpgsqlRaiseExceptionRule();
        var info = CreateInfo("P0001", "Заведующий кафедрой должен принадлежать к ней.");

        var result = rule.TryRecognize(info);

        Assert.NotNull(result);
        Assert.Equal(DatabaseBusinessErrorCodes.BusinessRuleViolation, result!.Code);
        Assert.Equal("Заведующий кафедрой должен принадлежать к ней.", result.Message);
        Assert.Same(info, result.Raw);
    }

    [Theory]
    [InlineData("23503")]
    [InlineData("23505")]
    [InlineData("23502")]
    [InlineData("")]
    public void TryRecognize_WithNonRaiseExceptionState_ReturnsNull(string sqlState)
    {
        var rule = new PlpgsqlRaiseExceptionRule();
        var info = CreateInfo(sqlState, "msg");

        Assert.Null(rule.TryRecognize(info));
    }

    [Fact]
    public void TryRecognize_WithBlankMessage_ReturnsNull()
    {
        var rule = new PlpgsqlRaiseExceptionRule();
        var info = CreateInfo("P0001", "   ");

        Assert.Null(rule.TryRecognize(info));
    }

    [Fact]
    public void TryRecognize_AcceptsRaiseExceptionEvenWhenHintPresent()
    {
        var rule = new PlpgsqlRaiseExceptionRule();
        var info = CreateInfo("P0001", "msg", hint: "SOME_OTHER_MARKER");

        var result = rule.TryRecognize(info);

        Assert.NotNull(result);
        Assert.Equal(DatabaseBusinessErrorCodes.BusinessRuleViolation, result!.Code);
    }
}
