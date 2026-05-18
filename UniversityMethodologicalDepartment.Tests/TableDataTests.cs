using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests;

/// <summary>
/// Тесты чтения данных из таблиц department, employee, faculty.
/// Выполняется SELECT * FROM {table}; критерий успеха — возврат хотя бы одной строки.
/// Требуется ConnectionStrings__DefaultConnection в окружении (.runsettings или системе).
/// </summary>
public sealed class TableDataTests
{
    private static string? GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    private static AppDbContext CreateDbContext()
    {
        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Fail("Переменная окружения ConnectionStrings__DefaultConnection должна быть установлена для выполнения теста.");
        }

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

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Department_SelectAll_ReturnsAtLeastOneRow()
    {
        await using var db = CreateDbContext();
        var rows = await db.Departments.ToListAsync(TestContext.Current.CancellationToken);
        Assert.True(rows.Count >= 1, "Таблица department должна содержать хотя бы одну запись.");
    }

    [Fact]
    public async Task Employee_SelectAll_ReturnsAtLeastOneRow()
    {
        await using var db = CreateDbContext();
        var rows = await db.Employees.ToListAsync(TestContext.Current.CancellationToken);
        Assert.True(rows.Count >= 1, "Таблица employee должна содержать хотя бы одну запись.");
    }

    [Fact]
    public async Task Faculty_SelectAll_ReturnsAtLeastOneRow()
    {
        await using var db = CreateDbContext();
        var rows = await db.Faculties.ToListAsync(TestContext.Current.CancellationToken);
        Assert.True(rows.Count >= 1, "Таблица faculty должна содержать хотя бы одну запись.");
    }
}
