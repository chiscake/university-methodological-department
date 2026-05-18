using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UniversityMethodologicalDepartment.App.Pages.Analytics;

public sealed partial class QueriesPage : Page
{
    public QueriesPageViewModel ViewModel { get; }

    public QueriesPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<QueriesPageViewModel>();
        DataContext = ViewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Departments.Count == 0)
        {
            await ViewModel.InitializeAsync();
        }

        if (ViewModel.Results.Count == 0 && !ViewModel.IsLoading)
        {
            await ViewModel.ExecuteQueryAsync();
        }
    }

}
