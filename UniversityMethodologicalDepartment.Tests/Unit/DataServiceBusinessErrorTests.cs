using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;
using UniversityMethodologicalDepartment.Tests.Unit.Helpers;

namespace UniversityMethodologicalDepartment.Tests.Unit;

/// <summary>
/// Юнит-тесты обработки бизнес-ошибок БД в <see cref="DataService"/>:
/// проверка, что распознанные ошибки превращаются в типизированные исключения,
/// а нераспознанные — в <c>false</c>, без обращения к реальной БД.
/// </summary>
public sealed class DataServiceBusinessErrorTests
{
    private static readonly Faculty SampleFaculty = new() { Id = 1, Name = "F", DeanId = 1 };

    private static IDatabaseErrorRecognizer CreateRecognizer() =>
        new DatabaseErrorRecognizer(new IDatabaseBusinessRule[]
        {
            new CurriculumLimitRule(),
            new PlpgsqlRaiseExceptionRule(),
        });

    private static PostgresException CreatePostgresException(string sqlState, string messageText, string? hint = null)
        => new(messageText: messageText, severity: "ERROR", invariantSeverity: "ERROR", sqlState: sqlState, detail: null, hint: hint);

    private static DataService CreateService<T>(IRepository<T> repository) where T : class
    {
        var auth = new FakeAuthService { IsAuthenticated = true };
        return new DataService(
            new InMemoryAppDbContextFactory(),
            new FixedRepositoryResolver<T>(repository),
            auth,
            CreateRecognizer());
    }

    [Fact]
    public async Task AddAsync_RecognizedCurriculumLimit_ThrowsCurriculumItemSemesterLimitExceededException()
    {
        var pg = CreatePostgresException("P0001", "Достигнут лимит 7 предметов на семестр.", CurriculumLimitRule.HintMarker);
        var dbUpdate = new DbUpdateException("save failed", pg);
        var repository = new ThrowingRepository<Faculty>(dbUpdate);
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<CurriculumItemSemesterLimitExceededException>(() =>
            service.AddAsync(SampleFaculty, TestContext.Current.CancellationToken));

        Assert.IsAssignableFrom<DatabaseBusinessException>(ex);
        Assert.Equal(DatabaseBusinessErrorCodes.CurriculumLimitExceeded, ex.BusinessError.Code);
        Assert.Equal("Достигнут лимит 7 предметов на семестр.", ex.Message);
        Assert.Same(dbUpdate, ex.InnerException);
    }

    [Fact]
    public async Task UpdateAsync_RecognizedCurriculumLimit_ThrowsCurriculumItemSemesterLimitExceededException()
    {
        var pg = CreatePostgresException("P0001", "Лимит превышен (update).", CurriculumLimitRule.HintMarker);
        var dbUpdate = new DbUpdateException("save failed", pg);
        var repository = new ThrowingRepository<Faculty>(dbUpdate);
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<CurriculumItemSemesterLimitExceededException>(() =>
            service.UpdateAsync(SampleFaculty, TestContext.Current.CancellationToken));

        Assert.Equal(DatabaseBusinessErrorCodes.CurriculumLimitExceeded, ex.BusinessError.Code);
        Assert.Equal("Лимит превышен (update).", ex.Message);
    }

    [Fact]
    public async Task AddAsync_GenericRaiseException_ThrowsDatabaseBusinessException_WithBusinessRuleViolationCode()
    {
        var pg = CreatePostgresException("P0001", "Заведующий кафедрой должен принадлежать к ней.");
        var dbUpdate = new DbUpdateException("save failed", pg);
        var repository = new ThrowingRepository<Faculty>(dbUpdate);
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<DatabaseBusinessException>(() =>
            service.AddAsync(SampleFaculty, TestContext.Current.CancellationToken));

        Assert.IsNotType<CurriculumItemSemesterLimitExceededException>(ex);
        Assert.Equal(DatabaseBusinessErrorCodes.BusinessRuleViolation, ex.BusinessError.Code);
        Assert.Equal("Заведующий кафедрой должен принадлежать к ней.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_GenericRaiseException_ThrowsDatabaseBusinessException_WithBusinessRuleViolationCode()
    {
        var pg = CreatePostgresException("P0001", "Бизнес-инвариант нарушен (update).");
        var dbUpdate = new DbUpdateException("save failed", pg);
        var repository = new ThrowingRepository<Faculty>(dbUpdate);
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<DatabaseBusinessException>(() =>
            service.UpdateAsync(SampleFaculty, TestContext.Current.CancellationToken));

        Assert.Equal(DatabaseBusinessErrorCodes.BusinessRuleViolation, ex.BusinessError.Code);
        Assert.Equal("Бизнес-инвариант нарушен (update).", ex.Message);
    }

    [Theory]
    [InlineData("23503")]
    [InlineData("23505")]
    public async Task AddAsync_NonBusinessSqlState_ReturnsFalse(string sqlState)
    {
        var pg = CreatePostgresException(sqlState, "constraint failure");
        var dbUpdate = new DbUpdateException("save failed", pg);
        var repository = new ThrowingRepository<Faculty>(dbUpdate);
        var service = CreateService(repository);

        var result = await service.AddAsync(SampleFaculty, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_NonRecognizedDbUpdateException_ReturnsFalse()
    {
        var dbUpdate = new DbUpdateException("save failed (no postgres exception inside)");
        var repository = new ThrowingRepository<Faculty>(dbUpdate);
        var service = CreateService(repository);

        var result = await service.UpdateAsync(SampleFaculty, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_RecognizedBusinessRuleViolation_ThrowsDatabaseBusinessException()
    {
        var pg = CreatePostgresException("P0001", "Удаление запрещено бизнес-правилом.");
        var dbUpdate = new DbUpdateException("save failed", pg);
        var repository = new ThrowingRepository<Faculty>(dbUpdate, entityToReturnOnGet: SampleFaculty);
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<DatabaseBusinessException>(() =>
            service.DeleteAsync<Faculty>(SampleFaculty.Id, TestContext.Current.CancellationToken));

        Assert.Equal(DatabaseBusinessErrorCodes.BusinessRuleViolation, ex.BusinessError.Code);
        Assert.Equal("Удаление запрещено бизнес-правилом.", ex.Message);
    }
}
