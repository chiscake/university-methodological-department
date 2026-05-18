namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Идентификатор и email текущего пользователя аутентификации.
/// </summary>
public sealed class UserInfo
{
    /// <summary>Уникальный идентификатор пользователя (например, Supabase auth.uid).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Email пользователя.</summary>
    public string? Email { get; init; }

    /// <summary>Роль пользователя (например, admin) из JWT/metadata.</summary>
    public string? Role { get; init; }
}
