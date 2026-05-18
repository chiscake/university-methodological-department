using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Pages.Admin;
using UniversityMethodologicalDepartment.App.Pages.Analytics;
using UniversityMethodologicalDepartment.App.Pages.Lists;
using UniversityMethodologicalDepartment.App.Views;

namespace UniversityMethodologicalDepartment.App.Pages.Home;

public sealed partial class HomePageViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    public ObservableCollection<HomeQuickAccessCard> ReferenceCards { get; } =
    [
        new("Факультеты", "Просмотр и ведение списка факультетов.", "\uE8F1", typeof(FacultiesPage)),
        new("Кафедры", "Список кафедр и переход к детальным данным.", "\uE8B7", typeof(DepartmentsPage)),
        new("Секции", "Управление справочником секций.", "\uE8A9", typeof(SectionPage)),
        new("Дисциплины", "Просмотр списка дисциплин по кафедрам.", "\uE8A5", typeof(DisciplinesPage)),
        new("Специальности", "Справочник образовательных специальностей.", "\uE8A5", typeof(SpecialtiesPage)),
        new("Учебный план", "Доступ к элементам учебного плана.", "\uE787", typeof(CurriculumsPage)),
        new("Сотрудники", "Справочник преподавателей и сотрудников.", "\uE716", typeof(EmployeesPage))
    ];

    public ObservableCollection<HomeQuickAccessCard> AnalyticsCards { get; } =
    [
        new("Запросы", "Быстрое выполнение аналитических запросов.", "\uE721", typeof(QueriesPage)),
        new("Диаграммы", "Визуализация показателей по данным.", "\uE908", typeof(ChartsPage)),
        new("Отчёты", "Формирование и экспорт отчётов.", "\uE8A5", typeof(ReportsPage))
    ];

    public ObservableCollection<HomeQuickAccessCard> AdminCards { get; } =
    [
        new("Журнал", "Журнал действий и системных событий.", "\uE8A1", typeof(JournalPage)),
        new("Управление пользователями", "Роли и доступы пользователей.", "\uE7EF", typeof(UserManagementPage))
    ];

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    public partial bool IsAdmin { get; set; }

    public Visibility AuthPromptVisibility => IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;

    public Visibility AdminSectionVisibility => IsAdmin ? Visibility.Visible : Visibility.Collapsed;

    public HomePageViewModel(IAuthService authService)
    {
        _authService = authService;
        RefreshAuthState();
    }

    public void RefreshAuthState()
    {
        IsAuthenticated = _authService.IsAuthenticated;
        IsAdmin = _authService.IsAdmin;
    }

    partial void OnIsAuthenticatedChanged(bool value)
    {
        OnPropertyChanged(nameof(AuthPromptVisibility));
    }

    partial void OnIsAdminChanged(bool value)
    {
        OnPropertyChanged(nameof(AdminSectionVisibility));
    }

    [RelayCommand]
    private void OpenCard(HomeQuickAccessCard? card)
    {
        if (card is null)
        {
            Debug.WriteLine("[HomePage] OpenCard command invoked with null card.");
            return;
        }

        Debug.WriteLine($"[HomePage] OpenCard command invoked. Target page: {card.PageType.FullName}.");

        if (App.MainWindow is MainWindow window)
        {
            Debug.WriteLine($"[HomePage] Navigating to: {card.PageType.Name}.");
            window.Navigate(card.PageType);
            return;
        }

        Debug.WriteLine("[HomePage] MainWindow is not available. Navigation skipped.");
    }

    [RelayCommand]
    private void GoToLogin()
    {
        if (App.MainWindow is MainWindow window)
            window.Navigate(typeof(LoginPage));
    }
}

public sealed record HomeQuickAccessCard(string Title, string Description, string Glyph, Type PageType);
