using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// Простой тестовый двойник <see cref="IAuthService"/> с настраиваемыми флагами и пользователем.
/// Не делает обращений к Supabase и не выбрасывает исключения из методов жизненного цикла.
/// </summary>
internal sealed class FakeAuthService : IAuthService
{
#pragma warning disable CS0067 // событие не используется в юнит-тестах
    public event EventHandler? AuthStateChanged;
#pragma warning restore CS0067

    public bool IsAuthenticated { get; set; }

    public UserInfo? CurrentUser { get; set; }

    public string? CurrentRole { get; set; }

    public bool IsAdmin { get; set; }

    public string? AccessToken { get; set; }

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
        => Task.FromResult(AuthResult.Failure("Not used in unit tests"));

    public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
