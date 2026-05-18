using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Pages.Home;

namespace UniversityMethodologicalDepartment.App.Views;

/// <summary>
/// Главная страница приложения с использованием DevWinUI MainLandingPage.
/// Заголовок и подзаголовок заданы в HeaderContent (белый текст + чёрная тень).
/// </summary>
public sealed partial class HomePage : Page
{
    public HomePageViewModel ViewModel { get; }

    private readonly IAuthService _authService;

    public HomePage()
    {
        ViewModel = App.Services.GetRequiredService<HomePageViewModel>();
        _authService = App.Services.GetRequiredService<IAuthService>();
        DataContext = ViewModel;
        InitializeComponent();
        Unloaded += HomePage_Unloaded;
        _authService.AuthStateChanged += AuthService_AuthStateChanged;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        mainLanding.HeaderImage = "ms-appx:///Assets/sl-p.jpg";
        ViewModel.RefreshAuthState();
    }

    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _authService.AuthStateChanged -= AuthService_AuthStateChanged;
        Unloaded -= HomePage_Unloaded;
    }

    private void AuthService_AuthStateChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ViewModel.RefreshAuthState);
    }

    private void QuickAccessCardButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: HomeQuickAccessCard card })
        {
            ViewModel.OpenCardCommand.Execute(card);
        }
    }
}
