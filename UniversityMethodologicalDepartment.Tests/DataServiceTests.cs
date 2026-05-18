using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Bus.Services.Errors;

namespace UniversityMethodologicalDepartment.Tests;

/// <summary>
/// Тесты чтения данных через <see cref="DataService"/> из таблиц department, employee, faculty.
/// Критерий успеха — возврат хотя бы одной строки. Требуется ConnectionStrings__DefaultConnection в окружении.
/// </summary>
public sealed class DataServiceTests
{
    private static string? GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    private static IDataService CreateDataService()
    {
        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Fail("Переменная окружения ConnectionStrings__DefaultConnection должна быть установлена для выполнения теста.");
        }

        var factory = new TestAppDbContextFactory(connectionString);
        var authService = new FakeAuthService();
        var repositoryResolver = new StubRepositoryResolver();

        var errorRecognizer = new DatabaseErrorRecognizer(Array.Empty<IDatabaseBusinessRule>());
        return new DataService(factory, repositoryResolver, authService, errorRecognizer);
    }

    [Fact]
    public async Task Department_GetDepartmentsAsync_ReturnsAtLeastOneRow()
    {
        var dataService = CreateDataService();
        var rows = await dataService.GetDepartmentsAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(rows.Count >= 1, "DataService.GetDepartmentsAsync() должен вернуть хотя бы одну запись (таблица department).");
    }

    [Fact]
    public async Task Employee_GetEmployeesAsync_ReturnsAtLeastOneRow()
    {
        var dataService = CreateDataService();
        var rows = await dataService.GetEmployeesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(rows.Count >= 1, "DataService.GetEmployeesAsync() должен вернуть хотя бы одну запись (таблица employee).");
    }

    [Fact]
    public async Task Faculty_GetFacultiesAsync_ReturnsAtLeastOneRow()
    {
        var dataService = CreateDataService();
        var rows = await dataService.GetFacultiesAsync(TestContext.Current.CancellationToken);
        Assert.True(rows.Count >= 1, "DataService.GetFacultiesAsync() должен вернуть хотя бы одну запись (таблица faculty).");
    }

    private sealed class TestAppDbContextFactory : IAppDbContextFactory
    {
        private readonly string _connectionString;

        public TestAppDbContextFactory(string connectionString) => _connectionString = connectionString;

        public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                })
                .Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            var provider = services.BuildServiceProvider();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseApplicationServiceProvider(provider)
                .UseRootApplicationServiceProvider(provider)
                .Options;

            return ValueTask.FromResult(new AppDbContext(options));
        }
    }

#pragma warning disable CS0067 // Event never used (test double)
    private sealed class FakeAuthService : IAuthService
    {
        public event EventHandler? AuthStateChanged;
#pragma warning restore CS0067
        public bool IsAuthenticated => false;
        public UserInfo? CurrentUser => null;

        public string? CurrentRole => throw new NotImplementedException();

        public bool IsAdmin => throw new NotImplementedException();

        public string? AccessToken => null;

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(AuthResult.Failure("Not used in tests"));

        public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubRepositoryResolver : IRepositoryResolver
    {
        public IRepository<T> GetRepository<T>() where T : class =>
            throw new NotSupportedException("IRepositoryResolver не используется в тестах чтения department/employee/faculty.");
    }
}
