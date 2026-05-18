namespace UniversityMethodologicalDepartment.App.Contracts;

/// <summary>Цель Windows Credential Manager для пользовательской строки подключения к PostgreSQL (отдельно от Supabase Auth).</summary>
public static class DatabaseConnectionCredentials
{
    public const string CredentialTarget = "UniversityMethodologicalDepartment:Database";
}
