using System.Text.Json;
using Helpers.Microsoft;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Сохранение и загрузка сессии Supabase Auth в локальных настройках приложения (ApplicationData.LocalSettings).
/// </summary>
public sealed class SupabaseSessionHandler : IGotrueSessionPersistence<Session>
{
    private const string SessionKey = "Supabase.Session";
    private readonly ISettingsProvider _settings = SettingsProviderFactory.CreateProvider();

    /// <summary>Сохраняет сессию в локальных настройках.</summary>
    public void SaveSession(Session session)
    {
        if (session == null)
        {
            return;
        }

        _settings.Set(SessionKey, JsonSerializer.Serialize(session));
    }

    /// <summary>Удаляет сохранённую сессию.</summary>
    public void DestroySession()
    {
        _settings.Set(SessionKey, string.Empty);
    }

    /// <summary>Загружает сессию из локальных настроек или возвращает null.</summary>
    public Session? LoadSession()
    {
        var json = _settings.Get<string>(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Session>(json);
        }
        catch (JsonException)
        {
            _settings.Set(SessionKey, string.Empty);
            return null;
        }
    }
}
