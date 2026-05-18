using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Tests.Unit.Helpers;

namespace UniversityMethodologicalDepartment.Tests.Unit;

/// <summary>
/// Юнит-тесты security-gate в <see cref="DataService"/>: при недостатке прав ни мутации, ни админ-методы
/// не должны обращаться к БД и репозиториям. Все тесты используют throwing-двойники, которые падают,
/// если будет вызвана фабрика контекста или резолвер репозиториев.
/// </summary>
public sealed class DataServiceAccessControlTests
{
    private static IDatabaseErrorRecognizer CreateNoOpRecognizer()
        => new DatabaseErrorRecognizer(Array.Empty<IDatabaseBusinessRule>());

    private static (DataService Service, ThrowingDbContextFactory Factory, ThrowingRepositoryResolver Resolver, FakeAuthService Auth)
        CreateUnauthenticatedSut()
    {
        var factory = new ThrowingDbContextFactory();
        var resolver = new ThrowingRepositoryResolver();
        var auth = new FakeAuthService { IsAuthenticated = false, IsAdmin = false };
        var service = new DataService(factory, resolver, auth, CreateNoOpRecognizer());
        return (service, factory, resolver, auth);
    }

    private static (DataService Service, ThrowingDbContextFactory Factory) CreateNonAdminAuthenticatedSut()
    {
        var factory = new ThrowingDbContextFactory();
        var resolver = new ThrowingRepositoryResolver();
        var auth = new FakeAuthService { IsAuthenticated = true, IsAdmin = false };
        var service = new DataService(factory, resolver, auth, CreateNoOpRecognizer());
        return (service, factory);
    }

    [Fact]
    public async Task AddAsync_WhenNotAuthenticated_ReturnsFalse_AndDoesNotTouchDb()
    {
        var (service, factory, _, _) = CreateUnauthenticatedSut();

        var result = await service.AddAsync(new Faculty { Name = "F", DeanId = 1 }, TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Equal(0, factory.CreateCallCount);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotAuthenticated_ReturnsFalse_AndDoesNotTouchDb()
    {
        var (service, factory, _, _) = CreateUnauthenticatedSut();

        var result = await service.UpdateAsync(new Faculty { Id = 1, Name = "F", DeanId = 1 }, TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Equal(0, factory.CreateCallCount);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotAuthenticated_ReturnsFalse_AndDoesNotTouchDb()
    {
        var (service, factory, _, _) = CreateUnauthenticatedSut();

        var result = await service.DeleteAsync<Faculty>(1, TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Equal(0, factory.CreateCallCount);
    }

    [Fact]
    public async Task GetAuditLogAsync_WhenNotAdmin_ReturnsEmpty_AndDoesNotTouchDb()
    {
        var (service, factory) = CreateNonAdminAuthenticatedSut();

        var result = await service.GetAuditLogAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Equal(0, factory.CreateCallCount);
    }

    [Fact]
    public void CanMutate_ReflectsAuthService_IsAuthenticated()
    {
        var factory = new ThrowingDbContextFactory();
        var resolver = new ThrowingRepositoryResolver();
        var auth = new FakeAuthService();
        var service = new DataService(factory, resolver, auth, CreateNoOpRecognizer());

        auth.IsAuthenticated = false;
        Assert.False(service.CanMutate);

        auth.IsAuthenticated = true;
        Assert.True(service.CanMutate);
    }
}
