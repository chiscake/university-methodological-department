using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;

namespace UniversityMethodologicalDepartment.Tests.Unit;

/// <summary>
/// Юнит-тесты <see cref="DatabaseErrorRecognizer"/>: порядок применения правил, обход InnerException
/// и поведение для нераспознанных кейсов.
/// </summary>
public sealed class DatabaseErrorRecognizerTests
{
    private static PostgresErrorInfo CreateInfo(string sqlState, string messageText, string? hint = null)
        => new(sqlState, messageText, hint, ConstraintName: null, ColumnName: null, TableName: null);

    private static IDatabaseErrorRecognizer CreateRecognizer() =>
        new DatabaseErrorRecognizer(new IDatabaseBusinessRule[]
        {
            new CurriculumLimitRule(),
            new PlpgsqlRaiseExceptionRule(),
        });

    [Fact]
    public void Recognize_HintMatch_ReturnsCurriculumLimitCode()
    {
        var recognizer = CreateRecognizer();
        var info = CreateInfo("P0001", "Лимит превышен", CurriculumLimitRule.HintMarker);

        var result = recognizer.Recognize(info);

        Assert.NotNull(result);
        Assert.Equal(DatabaseBusinessErrorCodes.CurriculumLimitExceeded, result!.Code);
        Assert.Equal("Лимит превышен", result.Message);
    }

    [Fact]
    public void Recognize_RaiseExceptionWithoutHint_FallsBackToBusinessRuleViolation()
    {
        var recognizer = CreateRecognizer();
        var info = CreateInfo("P0001", "Некий бизнес-инвариант нарушен.");

        var result = recognizer.Recognize(info);

        Assert.NotNull(result);
        Assert.Equal(DatabaseBusinessErrorCodes.BusinessRuleViolation, result!.Code);
        Assert.Equal("Некий бизнес-инвариант нарушен.", result.Message);
    }

    [Theory]
    [InlineData("23503")]
    [InlineData("23505")]
    public void Recognize_NonBusinessSqlState_ReturnsNull(string sqlState)
    {
        var recognizer = CreateRecognizer();
        var info = CreateInfo(sqlState, "constraint failure");

        Assert.Null(recognizer.Recognize(info));
    }

    [Fact]
    public void Recognize_EmptyRules_ReturnsNull()
    {
        var recognizer = new DatabaseErrorRecognizer(Array.Empty<IDatabaseBusinessRule>());
        var info = CreateInfo("P0001", "msg", CurriculumLimitRule.HintMarker);

        Assert.Null(recognizer.Recognize(info));
    }

    [Fact]
    public void Recognize_RulesEvaluatedInRegistrationOrder()
    {
        var firstWins = new StubRule(new DatabaseBusinessError("FIRST", "first", null));
        var secondWins = new StubRule(new DatabaseBusinessError("SECOND", "second", null));
        var recognizer = new DatabaseErrorRecognizer(new IDatabaseBusinessRule[] { firstWins, secondWins });

        var result = recognizer.Recognize(CreateInfo("P0001", "any"));

        Assert.NotNull(result);
        Assert.Equal("FIRST", result!.Code);
    }

    [Fact]
    public void Recognize_Exception_WithoutPostgresException_ReturnsNull()
    {
        var recognizer = CreateRecognizer();
        var ex = new InvalidOperationException("no postgres in here");

        Assert.Null(recognizer.Recognize(ex));
    }

    [Fact]
    public void Recognize_Exception_TraversesInnerExceptionChain()
    {
        var recognizer = new DatabaseErrorRecognizer(new IDatabaseBusinessRule[]
        {
            new MarkerRule(),
        });

        var inner = CreatePostgresException("Лимит превышен", CurriculumLimitRule.HintMarker);
        var middle = new InvalidOperationException("middle", inner);
        var outer = new InvalidOperationException("outer", middle);

        var result = recognizer.Recognize(outer);

        Assert.NotNull(result);
        Assert.Equal("MARKER", result!.Code);
        Assert.Equal("Лимит превышен", result.Message);
    }

    [Fact]
    public void Recognize_Exception_FindsCurriculumLimitViaHint()
    {
        var recognizer = CreateRecognizer();
        var pg = CreatePostgresException("Достигнут лимит 7 предметов на семестр.", CurriculumLimitRule.HintMarker);
        var dbUpdate = new Microsoft.EntityFrameworkCore.DbUpdateException("save failed", pg);

        var result = recognizer.Recognize(dbUpdate);

        Assert.NotNull(result);
        Assert.Equal(DatabaseBusinessErrorCodes.CurriculumLimitExceeded, result!.Code);
        Assert.Equal("Достигнут лимит 7 предметов на семестр.", result.Message);
    }

    private static Npgsql.PostgresException CreatePostgresException(string messageText, string? hint)
    {
        return new Npgsql.PostgresException(
            messageText: messageText,
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: "P0001",
            detail: null,
            hint: hint);
    }

    [Fact]
    public void Recognize_PostgresErrorInfo_NullThrows()
    {
        var recognizer = CreateRecognizer();
        Assert.Throws<ArgumentNullException>(() => recognizer.Recognize((PostgresErrorInfo)null!));
    }

    [Fact]
    public void Recognize_Exception_NullThrows()
    {
        var recognizer = CreateRecognizer();
        Assert.Throws<ArgumentNullException>(() => recognizer.Recognize((Exception)null!));
    }

    private sealed class StubRule : IDatabaseBusinessRule
    {
        private readonly DatabaseBusinessError _error;
        public StubRule(DatabaseBusinessError error) => _error = error;
        public DatabaseBusinessError? TryRecognize(PostgresErrorInfo info) => _error;
    }

    /// <summary>
    /// Вспомогательное правило: не зависит от <see cref="PostgresErrorInfo"/>, чтобы можно было
    /// проверить обход <see cref="Exception.InnerException"/> через подмену <see cref="Npgsql.PostgresException"/>
    /// собственным исключением, унаследованным от <see cref="Npgsql.PostgresException"/>, без построения SQL-стейта.
    /// </summary>
    private sealed class MarkerRule : IDatabaseBusinessRule
    {
        public DatabaseBusinessError? TryRecognize(PostgresErrorInfo info) =>
            new("MARKER", info.MessageText, info);
    }

}
