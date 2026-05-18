using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniversityMethodologicalDepartment.App.ViewModels;

namespace UniversityMethodologicalDepartment.App.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsPageViewModel>();
        DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += SettingsPage_Loaded;

#if DEBUG || DEBUG_UNPACKAGED
        hardResetCard.Visibility = Visibility.Visible;
#endif
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshConnectionSection();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsPageViewModel.ConnectionPasswordSurfaceVersion))
        {
            ConnectionStringPasswordBox.Password = string.Empty;
            ApplyConnectionPlaceholderOnly();
        }
        else if (e.PropertyName == nameof(SettingsPageViewModel.ShowConnectionMaskPlaceholder))
        {
            ApplyConnectionPlaceholderOnly();
        }
    }

    private void ApplyConnectionPlaceholderOnly()
    {
        ConnectionStringPasswordBox.PlaceholderText =
            ViewModel.ShowConnectionMaskPlaceholder ? "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022" : string.Empty;
    }

    private void ConnectionStringPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box)
        {
            ViewModel.OnConnectionDraftChanged(box.Password);
        }
    }

    private void ToCloneRepoCard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyGitCloneTextCommand.Execute(null);
    }
}
