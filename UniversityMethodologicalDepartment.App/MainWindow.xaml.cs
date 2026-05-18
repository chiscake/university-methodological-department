using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Helpers.Microsoft;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Helpers;
using UniversityMethodologicalDepartment.App.Pages.Analytics;
using UniversityMethodologicalDepartment.App.Services;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.App.Pages.Lists;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IReferenceSearchService _referenceSearchService;
    private IAuthService? _authService;
    private CancellationTokenSource? _searchDebounceCts;

    public NavigationView NavigationView => rootNavigationView;

    /// <summary>
    /// Сервис навигации для использования в коде и передачи в App.
    /// </summary>
    public INavigationService NavigationService => _navigationService;

    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _referenceSearchService = serviceProvider.GetRequiredService<IReferenceSearchService>();
        _navigationService = new NavigationService(rootFrame, serviceProvider);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        TrySetWindowTaskbarIcon();
    }

    /// <summary>
    /// Taskbar / Alt+Tab icon (TitleBar uses ms-appx icon in XAML).
    /// </summary>
    private void TrySetWindowTaskbarIcon()
    {
        var path = ResolveLogoIcoPath();
        if (path is not null)
            AppWindow.SetIcon(path);
    }

    private static string? ResolveLogoIcoPath()
    {
        const string relative = @"Assets\logo.ico";
        if (NativeMethods.IsAppPackaged)
        {
            try
            {
                var installed = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                if (!string.IsNullOrEmpty(installed))
                {
                    var packaged = Path.Combine(installed, relative);
                    if (File.Exists(packaged))
                        return packaged;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MainWindow] Package icon path lookup failed: " + ex.Message);
            }
        }

        var local = Path.Combine(AppContext.BaseDirectory, relative);
        return File.Exists(local) ? local : null;
    }

    public void Navigate(Type pageType, object? parameter = null)
    {
        _navigationService.Navigate(pageType, parameter);
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        _navigationService.GoBack();
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        rootNavigationView.IsPaneOpen = !rootNavigationView.IsPaneOpen;
    }

    private async void GlobalSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        // Capture on UI thread: after await we may be on a pool thread; AutoSuggestBox must be touched on UI.
        var uiQueue = sender.DispatcherQueue;

        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        try
        {
            await Task.Delay(250, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
                return;

            uiQueue.TryEnqueue(() =>
            {
                if (token.IsCancellationRequested)
                    return;
                _ = ApplyGlobalSearchDebouncedAsync(sender, token);
            });
        }
        catch (OperationCanceledException)
        {
            // debounce cancelled
        }
    }

    private async Task ApplyGlobalSearchDebouncedAsync(AutoSuggestBox sender, CancellationToken token)
    {
        var text = sender.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            sender.ItemsSource = null;
            return;
        }

        try
        {
            var results = await _referenceSearchService.SearchAsync(text, 20, ReferenceSearchScope.All, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
                return;

            var uiQueue = sender.DispatcherQueue;
            uiQueue.TryEnqueue(() =>
            {
                if (!token.IsCancellationRequested)
                    sender.ItemsSource = results;
            });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void GlobalSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ReferenceSearchItem item)
            NavigateFromSearchResult(sender, item);
    }

    private async void GlobalSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is ReferenceSearchItem chosen)
        {
            NavigateFromSearchResult(sender, chosen);
            return;
        }

        var uiQueue = sender.DispatcherQueue;
        var text = (sender.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
            return;

        try
        {
            var list = await _referenceSearchService.SearchAsync(text, 1, ReferenceSearchScope.All, CancellationToken.None).ConfigureAwait(false);
            if (list.Count > 0)
                uiQueue.TryEnqueue(() => NavigateFromSearchResult(sender, list[0]));
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[GlobalSearch] QuerySubmitted: " + ex.Message);
        }
    }

    private static void NavigateFromSearchResult(AutoSuggestBox sender, ReferenceSearchItem item)
    {
        ReferenceSearchNavigation.Open(item);
        sender.ItemsSource = null;
        sender.Text = string.Empty;
    }

    private void GlobalSearchAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        globalSearchBox?.Focus(FocusState.Programmatic);
        args.Handled = true;
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.ApplySystemThemeToCaptionButtons(this, rootGrid.ActualTheme);
    }

    private async void RootNavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        // Не трогаем PaneDisplayMode при загрузке — это вызывает сбой в Microsoft.ui.xaml.dll.
        // Сохранённый режим применяется при открытии страницы «Параметры».

        _authService ??= _serviceProvider.GetRequiredService<IAuthService>();
        _authService.AuthStateChanged -= AuthService_AuthStateChanged;
        _authService.AuthStateChanged += AuthService_AuthStateChanged;
        await _authService.EnsureInitializedAsync();
        await TryAutoSignInAsync();

        if (rootFrame.Content == null)
        {
            NavigateRoot(typeof(Views.HomePage), clearBackStack: true);
        }

        var queue = DispatcherQueue.GetForCurrentThread();
        queue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (rootNavigationView.SettingsItem is NavigationViewItem settingsItem)
                settingsItem.Content = "Параметры";
        });

        UpdateTitleBarAuthContent();
        UpdateAdminSectionVisibility();

        _ = LoadNavigationMenuDataAsync();
    }

    private async Task LoadNavigationMenuDataAsync()
    {
        var queue = DispatcherQueue.GetForCurrentThread();
        Debug.WriteLine("[LoadNavigationMenuDataAsync] Start loading faculties and departments.");
        try
        {
            var dataService = _serviceProvider.GetRequiredService<IDataService>();
            var faculties = await dataService.GetFacultiesAsync().ConfigureAwait(false);
            var departments = await dataService.GetDepartmentsAsync().ConfigureAwait(false);

            Debug.WriteLine($"[LoadNavigationMenuDataAsync] Result: faculties={faculties.Count}, departments={departments.Count}.");

            queue?.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PopulateNavigationMenu(faculties?.ToList() ?? new List<Faculty>(), departments?.ToList() ?? new List<Department>());
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoadNavigationMenuDataAsync] Error: {ex.Message}");
            queue?.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PopulateNavigationMenu(new List<Faculty>(), new List<Department>());
            });
        }
    }

    private void PopulateNavigationMenu(IList<Faculty> faculties, IList<Department> departments)
    {
        foreach (var item in rootNavigationView.MenuItems)
        {
            if (item is not NavigationViewItem nvi)
                continue;

            var tag = nvi.Tag?.ToString();
            if (tag == "Faculties")
            {
                nvi.MenuItems.Clear();
                foreach (var f in faculties)
                {
                    nvi.MenuItems.Add(new NavigationViewItem
                    {
                        Content = f.Name,
                        Tag = "Faculty:" + f.Id
                    });
                }
                Debug.WriteLine($"[PopulateNavigationMenu] Loaded {faculties.Count} faculties into nav panel: {string.Join(", ", faculties.Select(x => x.Name))}.");
            }
            else if (tag == "Departments")
            {
                nvi.MenuItems.Clear();
                foreach (var d in departments)
                {
                    nvi.MenuItems.Add(new NavigationViewItem
                    {
                        Content = d.Name,
                        Tag = "Department:" + d.Id
                    });
                }
                Debug.WriteLine($"[PopulateNavigationMenu] Loaded {departments.Count} departments into nav panel: {string.Join(", ", departments.Select(x => x.Name))}.");
            }
        }
    }

    private async Task TryAutoSignInAsync()
    {
        if (_authService is null || _authService.IsAuthenticated)
        {
            return;
        }

        var credentialStore = _serviceProvider.GetRequiredService<ICredentialStore>();
        var credential = credentialStore.Load(AuthService.CredentialsTarget);
        if (credential is null)
        {
            return;
        }

        await _authService.SignInAsync(credential.Email, credential.Password);
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            if (rootFrame.CurrentSourcePageType != typeof(Views.SettingsPage))
                Navigate(typeof(Views.SettingsPage));
            return;
        }

        var selected = args.SelectedItemContainer;
        if (selected == null) return;

        // По Tag или имени решаем, куда перейти
        var tag = (selected as NavigationViewItem)?.Tag?.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        if (tag == "Home")
        {
            if (rootFrame.CurrentSourcePageType != typeof(Views.HomePage))
                Navigate(typeof(Views.HomePage));
            return;
        }

        if (tag == "Faculties")
        {
            if (rootFrame.CurrentSourcePageType != typeof(FacultiesPage))
                Navigate(typeof(FacultiesPage));
            return;
        }

        if (tag == "Departments")
        {
            if (rootFrame.CurrentSourcePageType != typeof(DepartmentsPage))
                Navigate(typeof(DepartmentsPage));
            return;
        }

        if (tag.StartsWith("Faculty:", StringComparison.Ordinal))
        {
            if (int.TryParse(tag.AsSpan("Faculty:".Length), out var id))
            {
                var name = (selected as NavigationViewItem)?.Content?.ToString() ?? "";
                Navigate(typeof(FacultyDetailPage), new EntityDetailParameter(id, name));
            }
            return;
        }

        if (tag.StartsWith("Department:", StringComparison.Ordinal))
        {
            if (int.TryParse(tag.AsSpan("Department:".Length), out var id))
            {
                var name = (selected as NavigationViewItem)?.Content?.ToString() ?? "";
                Navigate(typeof(DepartmentDetailPage), new EntityDetailParameter(id, name));
            }
            return;
        }

        if (tag == "Sections")
        {
            if (rootFrame.CurrentSourcePageType != typeof(SectionPage))
                Navigate(typeof(SectionPage));
            return;
        }

        if (tag == "Disciplines")
        {
            if (rootFrame.CurrentSourcePageType != typeof(DisciplinesPage))
                Navigate(typeof(DisciplinesPage));
            return;
        }

        if (tag == "Specialties")
        {
            if (rootFrame.CurrentSourcePageType != typeof(SpecialtiesPage))
                Navigate(typeof(SpecialtiesPage));
            return;
        }

        if (tag == "Curriculums")
        {
            if (rootFrame.CurrentSourcePageType != typeof(CurriculumsPage))
                Navigate(typeof(CurriculumsPage));
            return;
        }

        if (tag == "Employees")
        {
            if (rootFrame.CurrentSourcePageType != typeof(EmployeesPage))
                Navigate(typeof(EmployeesPage));
            return;
        }

        if (tag == "AdminJournal")
        {
            if (rootFrame.CurrentSourcePageType != typeof(JournalPage))
                Navigate(typeof(JournalPage));
            return;
        }

        if (tag == "AdminUsers")
        {
            if (rootFrame.CurrentSourcePageType != typeof(UserManagementPage))
                Navigate(typeof(UserManagementPage));
            return;
        }

        if (tag == "Queries")
        {
            if (rootFrame.CurrentSourcePageType != typeof(QueriesPage))
                Navigate(typeof(QueriesPage));
            return;
        }

        if (tag == "Charts")
        {
            if (rootFrame.CurrentSourcePageType != typeof(ChartsPage))
                Navigate(typeof(ChartsPage));
            return;
        }

        if (tag == "Reports")
        {
            if (rootFrame.CurrentSourcePageType != typeof(ReportsPage))
                Navigate(typeof(ReportsPage));
            return;
        }
    }

    private void RootFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        var pageType = e.SourcePageType;
        if (pageType == typeof(Views.HomePage))
        {
            rootNavigationView.SelectedItem = homeItem;
            return;
        }
        if (pageType == typeof(Views.LoginPage))
        {
            rootNavigationView.SelectedItem = null;
            return;
        }
        if (pageType == typeof(Views.SettingsPage))
        {
            rootNavigationView.SelectedItem = rootNavigationView.SettingsItem;
            return;
        }
        if (pageType == typeof(FacultyDetailPage) && e.Parameter is EntityDetailParameter facultyParam)
        {
            var item = FindNavigationItemByTag("Faculty:" + facultyParam.Id);
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(DepartmentDetailPage) && e.Parameter is EntityDetailParameter deptParam)
        {
            var item = FindNavigationItemByTag("Department:" + deptParam.Id);
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(SectionPage))
        {
            var item = FindNavigationItemByTag("Sections");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(FacultiesPage))
        {
            var item = FindNavigationItemByTag("Faculties");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(DepartmentsPage))
        {
            var item = FindNavigationItemByTag("Departments");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(DisciplinesPage))
        {
            var item = FindNavigationItemByTag("Disciplines");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(SpecialtiesPage))
        {
            var item = FindNavigationItemByTag("Specialties");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(CurriculumsPage))
        {
            var item = FindNavigationItemByTag("Curriculums");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(EmployeesPage))
        {
            var item = FindNavigationItemByTag("Employees");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(JournalPage))
        {
            var item = FindNavigationItemByTag("AdminJournal");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(UserManagementPage))
        {
            var item = FindNavigationItemByTag("AdminUsers");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(QueriesPage))
        {
            var item = FindNavigationItemByTag("Queries");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(ChartsPage))
        {
            var item = FindNavigationItemByTag("Charts");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
        if (pageType == typeof(ReportsPage))
        {
            var item = FindNavigationItemByTag("Reports");
            if (item != null)
                rootNavigationView.SelectedItem = item;
            return;
        }
    }

    private NavigationViewItem? FindNavigationItemByTag(string tag)
    {
        foreach (var item in rootNavigationView.MenuItems)
        {
            if (item is NavigationViewItem nvi)
            {
                if (nvi.Tag?.ToString() == tag)
                    return nvi;
                foreach (var child in nvi.MenuItems)
                {
                    if (child is NavigationViewItem childNvi && childNvi.Tag?.ToString() == tag)
                        return childNvi;
                }
            }
        }
        return null;
    }

    private void UpdateTitleBarAuthContent()
    {
        var isAuthenticated = _authService?.IsAuthenticated == true;
        var user = _authService?.CurrentUser;

        var panel = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
            Spacing = 8
        };

        if (isAuthenticated && user != null)
        {
            var emailText = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = user.Email ?? user.Id,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            };
            var logoutButton = new Microsoft.UI.Xaml.Controls.Button
            {
                Content = "Выйти",
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            };
            logoutButton.Click += (_, _) => _authService?.SignOutAsync();
            panel.Children.Add(emailText);
            panel.Children.Add(logoutButton);
        }
        else
        {
            var loginButton = new Microsoft.UI.Xaml.Controls.Button
            {
                Content = "Войти",
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            };
            loginButton.Click += (_, _) =>
            {
                if (rootFrame.CurrentSourcePageType != typeof(Views.LoginPage))
                    Navigate(typeof(Views.LoginPage));
            };
            panel.Children.Add(loginButton);
        }

        titleBar.RightHeader = panel;
    }

    private void AuthService_AuthStateChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateTitleBarAuthContent();
            UpdateAdminSectionVisibility();
        });
    }

    private void UpdateAdminSectionVisibility()
    {
        var isAdmin = _authService?.IsAdmin == true;
        var visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

        adminHeader.Visibility = visibility;
        adminJournalItem.Visibility = visibility;
        adminUsersItem.Visibility = visibility;
    }

    private void NavigateRoot(Type pageType, bool clearBackStack)
    {
        rootFrame.Navigate(pageType);
        if (clearBackStack)
        {
            rootFrame.BackStack.Clear();
        }
    }
}
