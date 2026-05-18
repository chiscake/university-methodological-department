using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supabase;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests;

/// <summary>
/// Тесты проверки подключения к БД (EF Core, Npgsql) и авторизации через Supabase Auth.
/// Переменные окружения задаются через .runsettings или систему: ConnectionStrings__DefaultConnection,
/// Supabase__Url, Supabase__AnonKey. При их отсутствии тесты падают с явным сообщением.
/// </summary>
public sealed class DatabaseConnectionTests
{
    private const string TestUserEmail = "test@example.com";
    private const string TestUserPassword = "12345678";

    private static string? GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    private static (string? Url, string? AnonKey) GetSupabaseConfig() => (
        Environment.GetEnvironmentVariable("Supabase__Url"),
        Environment.GetEnvironmentVariable("Supabase__AnonKey")
    );

    [Fact]
    public async Task CanConnectAsync_WhenConnectionStringSet_ReturnsTrue()
    {
        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Fail("Переменная окружения ConnectionStrings__DefaultConnection должна быть установлена для выполнения теста.");
        }

        // AppDbContext использует "Name=DefaultConnection" в OnConfiguring — передаём IConfiguration через ServiceProvider.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseApplicationServiceProvider(provider)
            .UseRootApplicationServiceProvider(provider)
            .Options;

        await using var db = new AppDbContext(options);
        var canConnect = await db.Database.CanConnectAsync(TestContext.Current.CancellationToken);

        Assert.True(canConnect, "Подключение к БД должно быть успешным при корректной строке подключения.");
    }

    [Fact]
    public async Task SignIn_WithTestUser_Succeeds()
    {
        var (url, anonKey) = GetSupabaseConfig();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(anonKey))
        {
            Assert.Fail("Переменные окружения Supabase__Url и Supabase__AnonKey должны быть установлены для выполнения теста.");
        }

        var client = new Client(url, anonKey);
        await client.InitializeAsync();

        var session = await client.Auth.SignIn(TestUserEmail, TestUserPassword);

        Assert.NotNull(session);
        Assert.NotNull(session.User);
        Assert.NotNull(session.AccessToken);
        Assert.Equal(TestUserEmail, session.User.Email, StringComparer.OrdinalIgnoreCase);
    }
}
