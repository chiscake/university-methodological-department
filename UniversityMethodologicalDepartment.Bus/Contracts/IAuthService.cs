namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Сервис аутентификации: инициализация клиента, вход/выход, текущий пользователь и событие смены состояния.
/// </summary>
public interface IAuthService
{
    /// <summary>Событие при изменении состояния аутентификации.</summary>
    event EventHandler? AuthStateChanged;

    /// <summary>Признак того, что пользователь аутентифицирован.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Текущий пользователь или null.</summary>
    UserInfo? CurrentUser { get; }

    /// <summary>Текущая роль пользователя из JWT/metadata или null.</summary>
    string? CurrentRole { get; }

    /// <summary>Признак администратора (роль admin).</summary>
    bool IsAdmin { get; }

    /// <summary>Текущий access JWT Supabase для вызова Edge Functions от имени пользователя, или null.</summary>
    string? AccessToken { get; }

    /// <summary>Гарантирует инициализацию и при необходимости восстанавливает сессию.</summary>
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>Выполняет вход по email и паролю.</summary>
    Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Выход из учётной записи и удаление сохранённых учётных данных.</summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);
}
