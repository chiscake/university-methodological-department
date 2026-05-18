using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.App.Pages.Admin;

namespace UniversityMethodologicalDepartment.App.Views;

public sealed partial class JournalPage : Page
{
    public JournalPageViewModel ViewModel { get; }

    public JournalPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<JournalPageViewModel>();
        DataContext = ViewModel;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Items.Count == 0 && !ViewModel.IsLoading)
        {
            _ = ViewModel.LoadAsync();
        }
    }
}
