using Microsoft.EntityFrameworkCore.Diagnostics;
using UniversityMethodologicalDepartment.Bus.Entities.Extensions;
using UniversityMethodologicalDepartment.Tests.Unit.Helpers;

namespace UniversityMethodologicalDepartment.Tests.Unit;

public sealed class SupabaseRlsInterceptorTests
{
    [Fact]
    public void Anon_PrependsSetLocalRoleAnon_AndEmptyClaims()
    {
        var interceptor = new SupabaseRlsInterceptor(userId: null);
        var command = new TestDbCommand { CommandText = "SELECT 1;" };

        _ = interceptor.NonQueryExecuting(
            command,
            eventData: null!,
            result: default);

        Assert.StartsWith("SET LOCAL ROLE anon; SET LOCAL request.jwt.claims = '{}';", command.CommandText, StringComparison.Ordinal);
        Assert.EndsWith("SELECT 1;", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void Authenticated_PrependsSetLocalRoleAuthenticated_AndSubClaim()
    {
        var interceptor = new SupabaseRlsInterceptor(userId: "abc");
        var command = new TestDbCommand { CommandText = "UPDATE t SET x = 1;" };

        _ = interceptor.NonQueryExecuting(command, eventData: null!, result: default);

        Assert.Contains("SET LOCAL ROLE authenticated;", command.CommandText, StringComparison.Ordinal);
        Assert.Contains("SET LOCAL request.jwt.claim.sub = 'abc';", command.CommandText, StringComparison.Ordinal);
        Assert.Contains("SET LOCAL request.jwt.claims = '{\"sub\":\"abc\"}';", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void Authenticated_WithAppRole_AddsAppMetadataRole()
    {
        var interceptor = new SupabaseRlsInterceptor(userId: "abc", appRole: "admin");
        var command = new TestDbCommand { CommandText = "DELETE FROM x;" };

        _ = interceptor.NonQueryExecuting(command, eventData: null!, result: default);

        Assert.Contains("\"app_metadata\":{\"role\":\"admin\"}", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void EscapesSingleQuotes_InUserId_AndClaimsJson()
    {
        var interceptor = new SupabaseRlsInterceptor(userId: "O'Brien");
        var command = new TestDbCommand { CommandText = "SELECT now();" };

        _ = interceptor.NonQueryExecuting(command, eventData: null!, result: default);

        Assert.Contains("request.jwt.claim.sub = 'O''Brien';", command.CommandText, StringComparison.Ordinal);
        var escapedAsDoubleQuote = command.CommandText.Contains("\"sub\":\"O''Brien\"", StringComparison.Ordinal);
        var escapedAsUnicode = command.CommandText.Contains("\"sub\":\"O\\u0027Brien\"", StringComparison.Ordinal);
        Assert.True(escapedAsDoubleQuote || escapedAsUnicode);
    }

    [Fact]
    public async Task Async_PathPrependsSameSetupSql()
    {
        var interceptor = new SupabaseRlsInterceptor(userId: "abc");
        var command = new TestDbCommand { CommandText = "SELECT 42;" };

        _ = await interceptor.NonQueryExecutingAsync(
            command,
            eventData: null!,
            result: default,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("SET LOCAL ROLE authenticated;", command.CommandText, StringComparison.Ordinal);
        Assert.EndsWith("SELECT 42;", command.CommandText, StringComparison.Ordinal);
    }
}
