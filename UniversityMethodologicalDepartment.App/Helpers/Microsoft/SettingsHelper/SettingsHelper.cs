using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Helpers.Microsoft;

public partial class SettingsHelper : ObservableSettings
{
    private static readonly SettingsHelper instance = new(SettingsProviderFactory.CreateProvider());
    public static SettingsHelper Current => instance;

    private SettingsHelper(ISettingsProvider provider)
        : base(provider)
    {
    }
    public const int MaxRecentlyVisitedSamples = 7;

    public ElementTheme SelectedAppTheme
    {
        get => GetOrCreateDefault(ElementTheme.Default);
        set => Set(value);
    }

    public bool IsLeftMode
    {
        get => GetOrCreateDefault(true);
        set => Set(value);
    }

    public bool IsShowCopyLinkTeachingTip
    {
        get => GetOrCreateDefault(true);
        set => Set(value);
    }

    public int ListPageSize
    {
        get => GetOrCreateDefault(50);
        set => Set(value <= 0 ? 50 : value);
    }

    public List<string> RecentlyVisited
    {
        get => GetOrCreateDefault(new List<string>()) ?? new List<string>();
        private set => Set(value);
    }

    public List<string> Favorites
    {
        get => GetOrCreateDefault(new List<string>()) ?? new List<string>();
        private set => Set(value);
    }

    public bool IsFirstRun
    {
        get => GetOrCreateDefault(true);
        set => Set(value);
    }

    public void UpdateFavorites(Action<List<string>> updater)
    {
        var list = Favorites;
        updater(list);
        Favorites = list;
    }
    public void UpdateRecentlyVisited(Action<List<string>> updater)
    {
        var list = RecentlyVisited;
        updater(list);
        RecentlyVisited = list;
    }

    /// <summary>Clears all settings to default state. Call before app exit so next launch is "untouched".</summary>
    public void ResetToDefaults() => ClearAllSettings();
}
