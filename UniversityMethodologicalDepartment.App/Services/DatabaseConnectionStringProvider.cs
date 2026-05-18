using System;
using Microsoft.Extensions.Configuration;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

public sealed class DatabaseConnectionStringProvider : IDatabaseConnectionStringProvider
{
    private readonly IConfiguration _configuration;
    private readonly ICredentialStore _credentialStore;

    public DatabaseConnectionStringProvider(IConfiguration configuration, ICredentialStore credentialStore)
    {
        _configuration = configuration;
        _credentialStore = credentialStore;
    }

    public bool HasUserOverride =>
        !string.IsNullOrWhiteSpace(_credentialStore.LoadSecret(DatabaseConnectionCredentials.CredentialTarget));

    public string? GetConfigurationConnectionString() =>
        _configuration.GetConnectionString("DefaultConnection");

    public string? GetUserOverrideConnectionString() =>
        _credentialStore.LoadSecret(DatabaseConnectionCredentials.CredentialTarget);

    public string GetEffectiveConnectionString()
    {
        var user = GetUserOverrideConnectionString();
        if (!string.IsNullOrWhiteSpace(user))
        {
            return user;
        }

        var config = GetConfigurationConnectionString();
        if (string.IsNullOrWhiteSpace(config))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        return config;
    }

    public bool SetUserOverride(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return _credentialStore.SaveSecret(DatabaseConnectionCredentials.CredentialTarget, connectionString.Trim());
    }

    public bool ClearUserOverride() =>
        _credentialStore.Delete(DatabaseConnectionCredentials.CredentialTarget);
}
