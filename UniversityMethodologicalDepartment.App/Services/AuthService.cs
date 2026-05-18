using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Supabase;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Сервис аутентификации через Supabase Auth: вход/выход, хранение сессии и учётных данных в Windows Credential Manager.
/// </summary>
public sealed class AuthService : IAuthService
{
    /// <summary>Имя цели в хранилище учётных данных для сохранения email/пароля.</summary>
    public const string CredentialsTarget = "UniversityMethodologicalDepartment";

    private readonly Client _supabase;
    private readonly ICredentialStore _credentialStore;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private readonly object _stateLock = new();
    private bool _initialized;
    private UserInfo? _currentUser;
    private string? _currentRole;

    public AuthService(Client supabase, ICredentialStore credentialStore)
    {
        _supabase = supabase;
        _credentialStore = credentialStore;
        _supabase.Auth.AddStateChangedListener((_, _) =>
        {
            SyncAuthState();
            AuthStateChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>Событие при изменении состояния аутентификации.</summary>
    public event EventHandler? AuthStateChanged;

    /// <summary>Признак того, что пользователь аутентифицирован.</summary>
    public bool IsAuthenticated => _supabase.Auth.CurrentSession is not null && _supabase.Auth.CurrentUser is not null;

    /// <summary>Текущий пользователь или null, если не аутентифицирован.</summary>
    public UserInfo? CurrentUser
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUser;
            }
        }
    }

    /// <summary>Текущая роль пользователя из JWT/metadata или null.</summary>
    public string? CurrentRole
    {
        get
        {
            lock (_stateLock)
            {
                return _currentRole;
            }
        }
    }

    /// <summary>Признак администратора (роль admin).</summary>
    public bool IsAdmin => string.Equals(CurrentRole, "admin", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? AccessToken => _supabase.Auth.CurrentSession?.AccessToken;

    /// <summary>Гарантирует инициализацию клиента Supabase и при необходимости восстанавливает сессию.</summary>
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await _supabase.InitializeAsync();
            _initialized = true;

            if (_supabase.Auth.CurrentSession is not null)
            {
                await _supabase.Auth.RetrieveSessionAsync();
            }

            SyncAuthState();
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    /// <summary>Выполняет вход по email и паролю; при успехе сохраняет учётные данные в хранилище.</summary>
    public async Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failure("Введите email и пароль.");
        }

        try
        {
            var session = await _supabase.Auth.SignIn(email, password);
            SyncAuthState(session);
            var user = CurrentUser;

            if (user is null)
            {
                return AuthResult.Failure("Не удалось получить данные пользователя.");
            }

            _credentialStore.Save(CredentialsTarget, email, password);
            return AuthResult.Success(user);
        }
        catch
        {
            return AuthResult.Failure("Неверный email или пароль.");
        }
    }

    /// <summary>Выход из учётной записи и удаление сохранённых учётных данных.</summary>
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync(cancellationToken);
        await _supabase.Auth.SignOut(default);
        SyncAuthState();
        _credentialStore.Delete(CredentialsTarget);
    }

    private void SyncAuthState(Supabase.Gotrue.Session? session = null)
    {
        var currentSession = session ?? _supabase.Auth.CurrentSession;
        var currentUser = _supabase.Auth.CurrentUser;

        var role = ExtractRoleFromAccessToken(currentSession?.AccessToken)
            ?? ExtractRoleFromUserMetadata(currentUser);

        lock (_stateLock)
        {
            _currentRole = role;
            _currentUser = MapUser(currentUser, role);
        }

        Debug.WriteLine(
            $"[AuthService] SyncAuthState: appRole={role ?? "null"}, IsAdmin={string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)}, session={(currentSession is not null ? "yes" : "no")}");
    }

    private static UserInfo? MapUser(Supabase.Gotrue.User? user, string? role)
    {
        if (user is null)
        {
            return null;
        }

        return new UserInfo
        {
            Id = user.Id ?? string.Empty,
            Email = user.Email,
            Role = role
        };
    }

    private static string? ExtractRoleFromAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payloadBytes = DecodeBase64Url(parts[1]);
            using var json = JsonDocument.Parse(payloadBytes);
            var root = json.RootElement;

            // JWT "role" at root is the Postgres role (anon / authenticated / service_role), not app_metadata.role.
            return TryReadRoleFromNested(root, "app_metadata", "role")
                ?? TryReadRoleFromNested(root, "user_metadata", "role")
                ?? TryReadRoleFromJson(root, "role");
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractRoleFromUserMetadata(Supabase.Gotrue.User? user)
    {
        if (user is null)
        {
            return null;
        }

        return TryReadRoleByProperty(user, "Role")
            ?? TryReadRoleByProperty(user, "AppMetadata")
            ?? TryReadRoleByProperty(user, "RawAppMetaData")
            ?? TryReadRoleByProperty(user, "UserMetadata")
            ?? TryReadRoleByProperty(user, "RawUserMetaData");
    }

    private static string? TryReadRoleByProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(instance);
        return TryReadRoleFromObject(value);
    }

    private static string? TryReadRoleFromObject(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string roleString)
        {
            if (string.IsNullOrWhiteSpace(roleString))
            {
                return null;
            }

            // Если строка похожа на JSON, пытаемся прочитать role как ключ.
            if (roleString.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                try
                {
                    using var json = JsonDocument.Parse(roleString);
                    return TryReadRoleFromJson(json.RootElement, "role");
                }
                catch
                {
                    // Fall through to plain-string role.
                }
            }

            return roleString.Trim();
        }

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is string key && string.Equals(key, "role", StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Value?.ToString();
                }
            }

            return null;
        }

        if (value is JsonElement jsonElement)
        {
            return TryReadRoleFromJson(jsonElement, "role");
        }

        // Fallback: пробуем достать поле/свойство Role у произвольного объекта.
        return TryReadRoleByProperty(value, "Role");
    }

    private static string? TryReadRoleFromNested(JsonElement root, string objectName, string roleKey)
    {
        if (root.TryGetProperty(objectName, out var nested) && nested.ValueKind == JsonValueKind.Object)
        {
            return TryReadRoleFromJson(nested, roleKey);
        }

        return null;
    }

    private static string? TryReadRoleFromJson(JsonElement element, string roleKey)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (!string.Equals(prop.Name, roleKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (prop.Value.ValueKind == JsonValueKind.String)
            {
                return prop.Value.GetString();
            }

            return prop.Value.ToString();
        }

        return null;
    }

    private static byte[] DecodeBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        var remainder = base64.Length % 4;
        if (remainder > 0)
        {
            base64 = base64.PadRight(base64.Length + (4 - remainder), '=');
        }

        return Convert.FromBase64String(base64);
    }
}
