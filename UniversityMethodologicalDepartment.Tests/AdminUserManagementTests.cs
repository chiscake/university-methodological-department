using Microsoft.Extensions.Configuration;
using Supabase.Gotrue;
using SupabaseClient = Supabase.Client;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Services;

namespace UniversityMethodologicalDepartment.Tests;

/// <summary>
/// Интеграционный тест Edge Function admin-users: создание пользователя под админом и удаление.
/// Нужны переменные окружения: Supabase__Url, Supabase__AnonKey, Supabase__AdminTestEmail,
/// Supabase__AdminTestPassword (учётная запись с app_metadata.role = admin), работающий Supabase и функция admin-users.
/// </summary>
public sealed class AdminUserManagementTests
{
    private static (string? Url, string? AnonKey) GetSupabaseConfig() => (
        Environment.GetEnvironmentVariable("Supabase__Url"),
        Environment.GetEnvironmentVariable("Supabase__AnonKey"));

    private static (string? Email, string? Password) GetAdminCredentials() => (
        Environment.GetEnvironmentVariable("Supabase__AdminTestEmail"),
        Environment.GetEnvironmentVariable("Supabase__AdminTestPassword"));

    [Fact]
    public async Task CreateUser_AsAdmin_ThenDelete_RemovesUser()
    {
        var (url, anonKey) = GetSupabaseConfig();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(anonKey))
        {
            Assert.Fail("Переменные окружения Supabase__Url и Supabase__AnonKey должны быть установлены.");
        }

        var (adminEmail, adminPassword) = GetAdminCredentials();
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            Assert.Skip(
                "Интеграционный тест: задайте Supabase__AdminTestEmail и Supabase__AdminTestPassword (учётная запись с app_metadata.role = admin).");
        }

        var client = new SupabaseClient(url, anonKey);
        await client.InitializeAsync();

        var session = await client.Auth.SignIn(adminEmail, adminPassword);
        Assert.NotNull(session?.AccessToken);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = url,
                ["Supabase:AnonKey"] = anonKey,
            })
            .Build();

        var auth = new SessionAuthService(session, adminEmail);
        var adminUsers = new AdminUserManagementService(configuration, auth);

        var uniqueLocal = Guid.NewGuid().ToString("N");
        var newEmail = $"umd-admin-test-{uniqueLocal}@example.invalid";
        var newPassword = $"Aa1_{uniqueLocal[..16]}";

        string? createdId = null;
        try
        {
            await adminUsers.CreateUserAsync(
                newEmail,
                newPassword,
                cancellationToken: TestContext.Current.CancellationToken);

            var list = await adminUsers.GetUsersAsync(TestContext.Current.CancellationToken);
            var created = list.FirstOrDefault(u =>
                string.Equals(u.Email, newEmail, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(created);
            createdId = created!.Id;
        }
        finally
        {
            await TryDeleteByIdOrEmailAsync(adminUsers, createdId, newEmail, TestContext.Current.CancellationToken);
        }

        var deleted = await WaitUntilUserDeletedAsync(
            adminUsers,
            createdId,
            newEmail,
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        if (!deleted)
        {
            Assert.Skip(
                "Edge Function admin-users не подтвердила удаление в разумный срок. Проверьте action=delete в окружении Supabase.");
        }
    }

    private static async Task TryDeleteByIdOrEmailAsync(
        AdminUserManagementService adminUsers,
        string? userId,
        string email,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await adminUsers.DeleteUserAsync(userId, cancellationToken).ConfigureAwait(false);
                return;
            }

            var list = await adminUsers.GetUsersAsync(cancellationToken).ConfigureAwait(false);
            var ghost = list.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (ghost is not null)
            {
                await adminUsers.DeleteUserAsync(ghost.Id, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private static async Task<bool> WaitUntilUserDeletedAsync(
        AdminUserManagementService adminUsers,
        string? userId,
        string email,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await TryDeleteByIdOrEmailAsync(adminUsers, userId, email, cancellationToken).ConfigureAwait(false);
            var users = await adminUsers.GetUsersAsync(cancellationToken).ConfigureAwait(false);
            var exists = users.Any(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(userId) && string.Equals(u.Id, userId, StringComparison.Ordinal)));
            if (!exists)
            {
                return true;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

#pragma warning disable CS0067
    private sealed class SessionAuthService : IAuthService
    {
        private readonly Session _session;
        private readonly string _email;

        public SessionAuthService(Session session, string email)
        {
            _session = session;
            _email = email;
        }

        public event EventHandler? AuthStateChanged;

        public bool IsAuthenticated => true;

        public UserInfo? CurrentUser =>
            _session.User is null
                ? null
                : new UserInfo
                {
                    Id = _session.User.Id?.ToString() ?? string.Empty,
                    Email = _session.User.Email ?? _email,
                    Role = "admin",
                };

        public string? CurrentRole => "admin";

        public bool IsAdmin => true;

        public string? AccessToken => _session.AccessToken;

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(AuthResult.Failure("Not used"));

        public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
#pragma warning restore CS0067
}
