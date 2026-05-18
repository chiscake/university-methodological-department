using CredentialManagement;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Реализация <see cref="ICredentialStore"/> через Windows Credential Manager (CredentialManagement):
/// сохранение и загрузка email/пароля по target для автологина между сессиями приложения.
/// </summary>
public sealed class WindowsCredentialStore : ICredentialStore
{
    /// <summary>Фиксированное имя пользователя для generic-секретов (полная строка в Password).</summary>
    private const string SecretUsernamePlaceholder = "UniversityMethodologicalDepartment.DbConnection";

    public bool Save(string target, string email, string password)
    {
        var credential = new Credential
        {
            Target = target,
            Username = email,
            Password = password,
            Type = CredentialType.Generic,
            PersistanceType = PersistanceType.LocalComputer
        };

        return credential.Save();
    }

    public StoredCredential? Load(string target)
    {
        var credential = new Credential
        {
            Target = target,
            Type = CredentialType.Generic
        };

        if (!credential.Load())
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(credential.Username) || string.IsNullOrWhiteSpace(credential.Password))
        {
            return null;
        }

        return new StoredCredential(credential.Username, credential.Password);
    }

    public bool Delete(string target)
    {
        var credential = new Credential
        {
            Target = target,
            Type = CredentialType.Generic
        };

        return credential.Delete();
    }

    public bool SaveSecret(string target, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var credential = new Credential
        {
            Target = target,
            Username = SecretUsernamePlaceholder,
            Password = secret,
            Type = CredentialType.Generic,
            PersistanceType = PersistanceType.LocalComputer
        };

        return credential.Save();
    }

    public string? LoadSecret(string target)
    {
        var credential = new Credential
        {
            Target = target,
            Type = CredentialType.Generic
        };

        if (!credential.Load())
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(credential.Password))
        {
            return null;
        }

        return credential.Password;
    }
}
