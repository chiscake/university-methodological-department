using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Helpers.Microsoft;

public partial class ObservableSettings : INotifyPropertyChanged
{
    private readonly ISettingsProvider provider;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableSettings(ISettingsProvider provider)
    {
        this.provider = provider;
    }

    protected bool Set<T>(T value, [CallerMemberName] string propertyName = null!)
    {
        if (provider.Contains(propertyName))
        {
            var currentValue = provider.Get<T>(propertyName);
            if (Equals(currentValue, value))
                return false;
        }

        provider.Set(propertyName, value);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    [return: MaybeNull]
    protected T Get<T>([CallerMemberName] string propertyName = null!)
    {
        return provider.Get<T>(propertyName);
    }

    [return: MaybeNull]
    protected T GetOrCreateDefault<T>(T defaultValue, [CallerMemberName] string propertyName = null!)
    {
        if (!provider.Contains(propertyName))
            Set(defaultValue, propertyName);

        return Get<T>(propertyName);
    }

    /// <summary>
    /// Clears all settings from the underlying storage. After app restart, defaults will be used.
    /// </summary>
    public void ClearAllSettings() => provider.Clear();
}
