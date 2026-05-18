using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.Bus.Services;

public sealed class AdminUserManagementService
{
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;

    public AdminUserManagementService(IConfiguration configuration, IAuthService authService)
    {
        _configuration = configuration;
        _authService = authService;
    }

    public async Task<IReadOnlyList<AdminAuthUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        using var response = await InvokeFunctionAsync(new { action = "list" }, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var users = new List<AdminAuthUser>();
        if (!doc.RootElement.TryGetProperty("users", out var usersElement) || usersElement.ValueKind != JsonValueKind.Array)
        {
            return users;
        }

        foreach (var item in usersElement.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var email = item.TryGetProperty("email", out var emailElement) ? emailElement.GetString() ?? string.Empty : string.Empty;
            var createdAt = item.TryGetProperty("created_at", out var createdAtElement) ? createdAtElement.GetString() : null;
            var role = ExtractRole(item);
            var fullName = ExtractFullName(item);
            users.Add(new AdminAuthUser(id, email, role, createdAt, fullName));
        }

        return users;
    }

    public async Task CreateUserAsync(string email, string password, string? fullName = null, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        using var response = await InvokeFunctionAsync(
            new { action = "create", email, password, fullName },
            cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateUserAsync(string userId, string? email, string? password, string? fullName = null, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Не указан идентификатор пользователя.");
        }

        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPassword = !string.IsNullOrWhiteSpace(password);
        var hasFullName = !string.IsNullOrWhiteSpace(fullName);
        if (!hasEmail && !hasPassword && !hasFullName)
        {
            return;
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action"] = "update",
            ["userId"] = userId,
        };
        if (hasEmail)
        {
            payload["email"] = email!.Trim();
        }

        if (hasPassword)
        {
            payload["password"] = password!;
        }

        if (hasFullName)
        {
            payload["fullName"] = fullName!.Trim();
        }

        using var response = await InvokeFunctionAsync(payload, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Не указан идентификатор пользователя.");
        }

        using var response = await InvokeFunctionAsync(
            new { action = "delete", userId },
            cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private string GetAdminUsersFunctionUrl()
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        if (string.IsNullOrWhiteSpace(supabaseUrl))
        {
            throw new InvalidOperationException("Не настроен Supabase:Url.");
        }

        return supabaseUrl.TrimEnd('/') + "/functions/v1/admin-users";
    }

    private async Task<HttpResponseMessage> InvokeFunctionAsync(object payload, CancellationToken cancellationToken)
    {
        var accessToken = _authService.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Требуется повторный вход: нет токена доступа.");
        }

        var anonKey = _configuration["Supabase:AnonKey"];
        if (string.IsNullOrWhiteSpace(anonKey))
        {
            throw new InvalidOperationException("Не настроен Supabase:AnonKey.");
        }

        var url = GetAdminUsersFunctionUrl();
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", anonKey);
        request.Content = CreateJsonContent(payload);
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureAdmin()
    {
        if (!_authService.IsAdmin)
        {
            throw new InvalidOperationException("Операция доступна только администратору.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException($"Ошибка Edge Function admin-users ({(int)response.StatusCode}): {body}");
    }

    private static StringContent CreateJsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static string ExtractRole(JsonElement userElement)
    {
        if (!userElement.TryGetProperty("app_metadata", out var appMetadata) || appMetadata.ValueKind != JsonValueKind.Object)
        {
            return "user";
        }

        if (!appMetadata.TryGetProperty("role", out var roleElement))
        {
            return "user";
        }

        return roleElement.GetString() ?? "user";
    }

    private static string? ExtractFullName(JsonElement userElement)
    {
        if (!userElement.TryGetProperty("user_metadata", out var userMetadata) || userMetadata.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!userMetadata.TryGetProperty("full_name", out var fullNameElement))
        {
            return null;
        }

        return fullNameElement.GetString();
    }
}

public sealed record AdminAuthUser(string Id, string Email, string Role, string? CreatedAtUtc, string? FullName);
