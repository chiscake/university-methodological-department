using Microsoft.Extensions.Configuration;
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Tests.Unit.Helpers;

namespace UniversityMethodologicalDepartment.Tests.Unit;

public sealed class AdminUserManagementServiceGuardTests
{
    private static AdminUserManagementService CreateSut(FakeAuthService auth)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new AdminUserManagementService(configuration, auth);
    }

    [Fact]
    public async Task GetUsersAsync_WhenNotAdmin_Throws_InvalidOperationException()
    {
        var sut = CreateSut(new FakeAuthService { IsAdmin = false });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetUsersAsync(TestContext.Current.CancellationToken));

        Assert.Contains("только администратору", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateUserAsync_WhenNotAdmin_Throws()
    {
        var sut = CreateSut(new FakeAuthService { IsAdmin = false });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateUserAsync(
                "x@example.com",
                "Pass1234",
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNotAdmin_Throws()
    {
        var sut = CreateSut(new FakeAuthService { IsAdmin = false });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.UpdateUserAsync(
                "id-1",
                "x@example.com",
                null,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserIdEmpty_Throws_AndDoesNotCallHttp()
    {
        var sut = CreateSut(new FakeAuthService { IsAdmin = true, AccessToken = null });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.UpdateUserAsync(
                "   ",
                "x@example.com",
                null,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("идентификатор пользователя", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNoFieldsToUpdate_ReturnsWithoutThrow_AndDoesNotCallHttp()
    {
        var sut = CreateSut(new FakeAuthService { IsAdmin = true, AccessToken = null });

        await sut.UpdateUserAsync("id-1", "   ", null, cancellationToken: TestContext.Current.CancellationToken);
        await sut.UpdateUserAsync("id-1", null, " ", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNotAdmin_Throws()
    {
        var sut = CreateSut(new FakeAuthService { IsAdmin = false });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DeleteUserAsync("id-1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserIdEmpty_Throws()
    {
        var sut = CreateSut(new FakeAuthService { IsAdmin = true });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DeleteUserAsync(" ", TestContext.Current.CancellationToken));

        Assert.Contains("идентификатор пользователя", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
