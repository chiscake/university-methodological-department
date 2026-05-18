using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.App.Pages.Admin;

namespace UniversityMethodologicalDepartment.App.Views;

public sealed partial class UserManagementPage : Page
{
    public UserManagementPageViewModel ViewModel { get; }

    public UserManagementPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<UserManagementPageViewModel>();
        DataContext = ViewModel;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Users.Count == 0 && !ViewModel.IsLoading)
        {
            _ = ViewModel.LoadAsync();
        }
    }

    private void CreatePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box)
        {
            ViewModel.CreatePassword = box.Password;
        }
    }

    private void EditPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box)
        {
            ViewModel.EditPassword = box.Password;
        }
    }
}
