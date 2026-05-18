using System;
using Helpers.Microsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Bus.Services.Errors.Rules;
using UniversityMethodologicalDepartment.App.Bus.Repositories;
using UniversityMethodologicalDepartment.App.ViewModels;
using UniversityMethodologicalDepartment.App.ViewModels.Details;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Pages.Admin;
using UniversityMethodologicalDepartment.App.Services;
using UniversityMethodologicalDepartment.App.Views;
using Supabase;
using UniversityMethodologicalDepartment.App.Pages.Home;
using UniversityMethodologicalDepartment.App.Pages.Lists.Curriculums;
using UniversityMethodologicalDepartment.App.Pages.Lists.Departments;
using UniversityMethodologicalDepartment.App.Pages.Lists.Disciplines;
using UniversityMethodologicalDepartment.App.Pages.Lists.Employees;
using UniversityMethodologicalDepartment.App.Pages.Lists.Faculties;
using UniversityMethodologicalDepartment.App.Pages.Lists.Sections;
using UniversityMethodologicalDepartment.App.Pages.Lists.Specialties;
using UniversityMethodologicalDepartment.App.Pages.Analytics;

namespace UniversityMethodologicalDepartment.App;

public partial class App : Application
{
    internal static MainWindow MainWindow { get; private set; } = null!;

    internal static IConfiguration Configuration { get; private set; } = null!;

    internal static IServiceProvider Services { get; private set; } = ConfigureServices();

    public App()
    {
        InitializeComponent();
        UnhandledException += HandleExceptions;
    }

    private static ServiceProvider ConfigureServices()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory);

#if DEBUG
        builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.AddJsonFile("appsettings.dev.json", optional: false, reloadOnChange: true);
#else
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
#endif
        var configuration = builder.Build();
        Configuration = configuration;

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<SupabaseSessionHandler>();
        services.AddSingleton<ICredentialStore, WindowsCredentialStore>();
        services.AddSingleton<IDatabaseConnectionStringProvider, DatabaseConnectionStringProvider>();
        services.AddSingleton<IDatabaseConnectionTester, DatabaseConnectionTester>();
        services.AddSingleton<IAppDbContextFactory, UserAwareAppDbContextFactory>();
        services.AddSingleton<IRepositoryResolver, ServiceProviderRepositoryResolver>();
        services.AddSingleton<ICacheStorage, FileBackedCacheStorage>();

        var supabaseUrl = configuration["Supabase:Url"];
        var supabaseAnonKey = configuration["Supabase:AnonKey"];
        if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseAnonKey))
        {
            services.AddSingleton(sp =>
            {
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false,
                    SessionHandler = sp.GetRequiredService<SupabaseSessionHandler>()
                };

                return new Supabase.Client(supabaseUrl, supabaseAnonKey, options);
            });
        }

        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IAppSettings, AppSettingsService>();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<IAppInfo, AppInfoService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IAccessibilityService, AccessibilityService>();
        services.AddSingleton<IAppLifecycle, AppLifecycleService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IReferenceSearchService, ReferenceSearchService>();
        services.AddTransient<IQueryService, QueryService>();
        services.AddTransient<IChartDataService, ChartDataService>();
        services.AddTransient<IReportExportService, ReportExportService>();
        services.AddTransient<IQueryResultExportService, QueryResultExportService>();
        services.AddTransient(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddSingleton<IDatabaseErrorRecognizer>(_ =>
            new DatabaseErrorRecognizer(new IDatabaseBusinessRule[]
            {
                new CurriculumLimitRule(),
                new PlpgsqlRaiseExceptionRule(),
            }));
        services.AddTransient<DataService>();
        services.AddTransient<IDataService>(sp => new CachedDataService(
            sp.GetRequiredService<DataService>(),
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ICacheStorage>()));
        services.AddSingleton<IAuthService, AuthService>();

        services.AddTransient<HomePageViewModel>();
        services.AddTransient<LoginPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<FacultyDetailPageViewModel>();
        services.AddTransient<DepartmentDetailPageViewModel>();
        services.AddTransient<SectionDetailPageViewModel>();
        services.AddTransient<DisciplineDetailPageViewModel>();
        services.AddTransient<CurriculumItemDetailPageViewModel>();
        services.AddTransient<EmployeeDetailPageViewModel>();
        services.AddTransient<SpecialtyDetailPageViewModel>();
        services.AddTransient<FacultiesPageViewModel>();
        services.AddTransient<DepartmentsPageViewModel>();
        services.AddTransient<SectionsPageViewModel>();
        services.AddTransient<DisciplinesPageViewModel>();
        services.AddTransient<SpecialtiesPageViewModel>();
        services.AddTransient<CurriculumsPageViewModel>();
        services.AddTransient<EmployeesPageViewModel>();
        services.AddTransient<AdminUserManagementService>();
        services.AddTransient<JournalPageViewModel>();
        services.AddTransient<UserManagementPageViewModel>();
        services.AddTransient<QueriesPageViewModel>();
        services.AddTransient<ChartsPageViewModel>();
        services.AddTransient<ReportsPageViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow(Services);
        WindowHelper.TrackWindow(MainWindow);
        ThemeHelper.Initialize();
        MainWindow.Activate();
    }

    private void HandleExceptions(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (NativeMethods.IsAppPackaged)
        {
            e.Handled = true;

            var notification = new AppNotificationBuilder()
                .AddText("An exception was thrown.")
                .AddText($"Type: {e.Exception.GetType()}")
                .AddText($"Message: {e.Message}\r\n" +
                         $"HResult: {e.Exception.HResult}")
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}
