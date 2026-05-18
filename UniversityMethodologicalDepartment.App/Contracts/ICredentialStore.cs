namespace UniversityMethodologicalDepartment.App.Contracts;

/// <summary>
/// Учётные данные, сохранённые в хранилище (email и пароль для автологина).
/// </summary>
public sealed record StoredCredential(string Email, string Password);

/// <summary>
/// Абстракция хранилища учётных данных (например, Windows Credential Manager) для сохранения
/// и загрузки email/пароля при успешном входе и удаления при выходе (автологин между сессиями).
/// </summary>
public interface ICredentialStore
{
    bool Save(string target, string email, string password);

    StoredCredential? Load(string target);

    bool Delete(string target);

    /// <summary>Сохраняет произвольный секрет (например строку подключения к БД) как generic credential.</summary>
    bool SaveSecret(string target, string secret);

    /// <summary>Загружает секрет по цели; null если записи нет или данные пустые.</summary>
    string? LoadSecret(string target);
}
