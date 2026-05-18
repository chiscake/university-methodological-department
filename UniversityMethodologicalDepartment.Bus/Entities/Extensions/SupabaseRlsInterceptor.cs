using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UniversityMethodologicalDepartment.Bus.Entities.Extensions;

/// <summary>
/// Интерцептор EF Core, выставляющий перед каждым SQL-запросом роль и request.jwt.claims,
/// чтобы политики RLS в PostgreSQL работали как при запросах через Supabase API: anon — только чтение,
/// authenticated — полный доступ с учётом auth.uid() при необходимости.
/// </summary>
public sealed class SupabaseRlsInterceptor : DbCommandInterceptor
{
    private readonly string? _userId;
    private readonly string? _appRole;

    /// <summary>Создаёт интерцептор с указанным идентификатором пользователя для роли и JWT-claims.</summary>
    /// <param name="userId">Идентификатор пользователя (null — роль anon, иначе authenticated с sub в claims).</param>
    /// <param name="appRole">Роль приложения (например, admin) для app_metadata.role в JWT-claims.</param>
    public SupabaseRlsInterceptor(string? userId, string? appRole = null)
    {
        _userId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        _appRole = string.IsNullOrWhiteSpace(appRole) ? null : appRole;
    }

    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ApplySessionContext(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        return ReaderExecutingAsyncCore(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySessionContext(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return NonQueryExecutingAsyncCore(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ApplySessionContext(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        return ScalarExecutingAsyncCore(command, eventData, result, cancellationToken);
    }

    private void ApplySessionContext(DbCommand command)
    {
        using var setupCommand = command.Connection!.CreateCommand();
        setupCommand.Transaction = command.Transaction;
        setupCommand.CommandText = BuildSetupSql();
        _ = setupCommand.ExecuteNonQuery();
    }

    private async Task ApplySessionContextAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var setupCommand = command.Connection!.CreateCommand();
        setupCommand.Transaction = command.Transaction;
        setupCommand.CommandText = BuildSetupSql();
        _ = await setupCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsyncCore(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken)
    {
        await ApplySessionContextAsync(command, cancellationToken).ConfigureAwait(false);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<InterceptionResult<int>> NonQueryExecutingAsyncCore(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken)
    {
        await ApplySessionContextAsync(command, cancellationToken).ConfigureAwait(false);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<InterceptionResult<object>> ScalarExecutingAsyncCore(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken)
    {
        await ApplySessionContextAsync(command, cancellationToken).ConfigureAwait(false);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private string BuildSetupSql()
    {
        if (_userId is null)
        {
            return "SET ROLE anon; SELECT set_config('request.jwt.claim.sub', '', false); SELECT set_config('request.jwt.claims', '{}', false);";
        }

        var claimsPayload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sub"] = _userId
        };

        if (_appRole is not null)
        {
            claimsPayload["app_metadata"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["role"] = _appRole
            };
        }

        var claims = JsonSerializer.Serialize(claimsPayload);
        var escapedClaims = claims.Replace("'", "''", StringComparison.Ordinal);
        var escapedSub = _userId.Replace("'", "''", StringComparison.Ordinal);
        return $"SET ROLE authenticated; SELECT set_config('request.jwt.claim.sub', '{escapedSub}', false); SELECT set_config('request.jwt.claims', '{escapedClaims}', false);";
    }
}
