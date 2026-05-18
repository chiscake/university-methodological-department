using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Supabase;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Entities.Extensions;

namespace UniversityMethodologicalDepartment.Tests.RlsCrud;

/// <summary>
/// Строка БД (app_readonly), SignIn test@example.com для JWT sub, фабрика контекста с <see cref="SupabaseRlsInterceptor"/>,
/// трекинг созданных specialty id и удаление в <see cref="DisposeAsync"/>.
/// </summary>
public sealed class RlsCrudCollectionFixture : IAsyncLifetime, IAsyncDisposable
{
    private const string TestUserEmail = "test@example.com";
    private const string TestUserPassword = "12345678";

    private readonly ConcurrentBag<int> _trackedSpecialtyIds = new();

    public string ConnectionString { get; private set; } = "";

    /// <summary>JWT claim sub для <see cref="SupabaseRlsInterceptor"/> (после <see cref="InitializeAsync"/>).</summary>
    public string TestUserSub { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection must be set (e.g. via .runsettings).");
        }

        var url = Environment.GetEnvironmentVariable("Supabase__Url");
        var anonKey = Environment.GetEnvironmentVariable("Supabase__AnonKey");
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(anonKey))
        {
            throw new InvalidOperationException(
                "Supabase__Url and Supabase__AnonKey must be set for RlsCrud collection.");
        }

        ConnectionString = connectionString;

        var client = new Client(url, anonKey);
        await client.InitializeAsync().ConfigureAwait(false);
        var session = await client.Auth.SignIn(TestUserEmail, TestUserPassword).ConfigureAwait(false);
        if (session?.User is null || string.IsNullOrWhiteSpace(session.User.Id?.ToString()))
        {
            throw new InvalidOperationException("SignIn failed for RlsCrud tests (check seed-auth-user.js).");
        }

        TestUserSub = session.User.Id.ToString()!;
    }

    public ValueTask<AppDbContext> CreateDbContextAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new SupabaseRlsInterceptor(userId))
            .Options;

        return ValueTask.FromResult(new AppDbContext(userId, options));
    }

    public void TrackSpecialtyForCleanup(int specialtyId) => _trackedSpecialtyIds.Add(specialtyId);

    public async ValueTask DisposeAsync()
    {
        var ids = _trackedSpecialtyIds.ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        await using var db = await CreateDbContextAsync(TestUserSub).ConfigureAwait(false);
        await db.Specialties
            .Where(s => ids.Contains(s.Id))
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
    }
}
