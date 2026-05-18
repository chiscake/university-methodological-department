using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// Тестовый <see cref="IAppDbContextFactory"/>, создающий <see cref="AppDbContext"/> с фиктивной
/// строкой подключения. Фактического обращения к БД не происходит при условии, что вызывающий код
/// не доходит до операций ввода-вывода (в наших тестах ошибка бросается фейк-репозиторием раньше).
/// </summary>
/// <remarks>
/// Кэширует общий <see cref="IServiceProvider"/> и <see cref="DbContextOptions{TContext}"/>, чтобы
/// не превышать порог EF "20 IServiceProviders" при массовом прогоне тестов.
/// </remarks>
internal sealed class InMemoryAppDbContextFactory : IAppDbContextFactory
{
    private static readonly DbContextOptions<AppDbContext> SharedOptions = BuildSharedOptions();

    public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new AppDbContext(SharedOptions));

    private static DbContextOptions<AppDbContext> BuildSharedOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fake;Username=fake;Password=fake"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        var provider = services.BuildServiceProvider();

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseApplicationServiceProvider(provider)
            .UseRootApplicationServiceProvider(provider)
            .Options;
    }
}
