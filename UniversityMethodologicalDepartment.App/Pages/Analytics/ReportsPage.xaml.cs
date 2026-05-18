using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace UniversityMethodologicalDepartment.App.Pages.Analytics;

public sealed partial class ReportsPage : Page
{
    public ReportsPageViewModel ViewModel { get; }

    public ReportsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ReportsPageViewModel>();
        DataContext = ViewModel;
    }
}
