using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

public sealed class DatabaseConnectionTester : IDatabaseConnectionTester
{
    public async Task<(bool Success, string? ErrorMessage)> TestAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, "Connection string is empty.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString.Trim());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand("SELECT 1", connection);
            _ = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
