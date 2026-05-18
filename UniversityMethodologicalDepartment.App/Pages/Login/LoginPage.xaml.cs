using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using UniversityMethodologicalDepartment.App.ViewModels;

namespace UniversityMethodologicalDepartment.App.Views;

public sealed partial class LoginPage : Page
{
    public LoginPageViewModel ViewModel { get; }

    public LoginPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<LoginPageViewModel>();
        ViewModel.SignInSucceeded += ViewModel_SignInSucceeded;
        DataContext = ViewModel;
    }

    private void ViewModel_SignInSucceeded(object? sender, EventArgs e)
    {
        if (Frame is null)
        {
            return;
        }

        Frame.Navigate(typeof(HomePage));
        Frame.BackStack.Clear();
    }
}
