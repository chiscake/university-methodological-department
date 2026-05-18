using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Entities.Extensions;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Фабрика контекста БД: берёт строку подключения из
/// <see cref="IDatabaseConnectionStringProvider"/> (override в Credential Manager или
/// <c>ConnectionStrings:DefaultConnection</c>) и текущего пользователя из
/// <see cref="IAuthService"/>, создаёт <see cref="SupabaseRlsInterceptor"/> с userId и возвращает
/// <see cref="AppDbContext"/> с этим интерцептором для применения RLS (anon/authenticated).
/// </summary>
public sealed class UserAwareAppDbContextFactory : IAppDbContextFactory
{
    private readonly IDatabaseConnectionStringProvider _connectionStringProvider;
    private readonly IAuthService _authService;

    public UserAwareAppDbContextFactory(IDatabaseConnectionStringProvider connectionStringProvider, IAuthService authService)
    {
        _connectionStringProvider = connectionStringProvider;
        _authService = authService;
    }

    public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connectionString = _connectionStringProvider.GetEffectiveConnectionString();

        var sanitized = System.Text.RegularExpressions.Regex.Replace(connectionString, @"(Password=)[^;]*", "$1***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        Debug.WriteLine($"[UserAwareAppDbContextFactory] CreateDbContext: connection (sanitized): {sanitized}");

        var userId = _authService.CurrentUser?.Id;
        var appRole = _authService.CurrentRole;
        var interceptor = new SupabaseRlsInterceptor(userId, appRole);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(interceptor)
            .Options;

        return ValueTask.FromResult(new AppDbContext(userId, options));
    }
}
