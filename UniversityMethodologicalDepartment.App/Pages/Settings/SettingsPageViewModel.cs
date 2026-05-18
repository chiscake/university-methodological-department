using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helpers.Microsoft;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private const int MinListPageSize = 1;
    private const int MaxListPageSize = 500;

    private readonly IThemeService _themeService;
    private readonly IAppSettings _appSettings;
    private readonly IClipboardService _clipboardService;
    private readonly IAccessibilityService _accessibilityService;
    private readonly IAppLifecycle _appLifecycle;
    private readonly IAuthService _authService;
    private readonly IDatabaseConnectionStringProvider _connectionStringProvider;
    private readonly IDatabaseConnectionTester _connectionTester;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly bool _isInitialized;
    private bool _isUpdatingListPageSize;
    private string _connectionDraft = string.Empty;

    public SettingsPageViewModel(
        IThemeService themeService,
        IAppSettings appSettings,
        IAppInfo appInfo,
        IClipboardService clipboardService,
        IAccessibilityService accessibilityService,
        IAppLifecycle appLifecycle,
        IAuthService authService,
        IDatabaseConnectionStringProvider connectionStringProvider,
        IDatabaseConnectionTester connectionTester,
        IUiDispatcher uiDispatcher)
    {
        _themeService = themeService;
        _appSettings = appSettings;
        _clipboardService = clipboardService;
        _accessibilityService = accessibilityService;
        _appLifecycle = appLifecycle;
        _authService = authService;
        _connectionStringProvider = connectionStringProvider;
        _connectionTester = connectionTester;
        _uiDispatcher = uiDispatcher;

        Version = appInfo.Version;
        WinAppSdkRuntimeDetails = appInfo.WinAppSdkRuntimeDetails;

        SelectedThemeIndex = ToThemeIndex(_themeService.GetTheme());
        NavigationSelectedIndex = _appSettings.IsLeftMode ? 0 : 1;
        ListPageSize = _appSettings.ListPageSize;

        NavigationOrientationHelper.UpdateNavigationViewForElement(_appSettings.IsLeftMode);

        UpdateConnectionSourceDescription();
        ShowConnectionMaskPlaceholder = _connectionStringProvider.HasUserOverride;

        _isInitialized = true;
    }

    /// <summary>Инверсия <see cref="IsTestingConnection"/> для IsEnabled кнопки «Проверить».</summary>
    public bool IsConnectionTestIdle => !IsTestingConnection;

    public string Version { get; }

    public string WinAppSdkRuntimeDetails { get; }

    public string GitCloneText { get; } = "git clone https://github.com/chisCake/university-methodological-department";

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    [ObservableProperty]
    public partial int NavigationSelectedIndex { get; set; }

    [ObservableProperty]
    public partial int ListPageSize { get; set; }

    [ObservableProperty]
    public partial string ConnectionSourceDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowConnectionMaskPlaceholder { get; set; }

    /// <summary>Увеличивается при сбросе черновика к сохранённому виду (страница синхронизирует PasswordBox).</summary>
    [ObservableProperty]
    public partial int ConnectionPasswordSurfaceVersion { get; set; }

    [ObservableProperty]
    public partial string? ConnectionStatusMessage { get; set; }

    [ObservableProperty]
    public partial bool ConnectionStatusIsError { get; set; }

    [ObservableProperty]
    public partial bool HasConnectionStatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsTestingConnection { get; set; }

    /// <summary>Вызывается при загрузке страницы настроек.</summary>
    public void RefreshConnectionSection()
    {
        UpdateConnectionSourceDescription();
        ShowConnectionMaskPlaceholder = _connectionStringProvider.HasUserOverride;
        _connectionDraft = string.Empty;
        ConnectionPasswordSurfaceVersion++;
    }

    /// <summary>Обновляет черновик из PasswordBox.</summary>
    public void OnConnectionDraftChanged(string? password)
    {
        _connectionDraft = password ?? string.Empty;
        if (_connectionDraft.Length > 0)
        {
            ShowConnectionMaskPlaceholder = false;
        }
        else if (_connectionStringProvider.HasUserOverride)
        {
            ShowConnectionMaskPlaceholder = true;
        }
    }

    partial void OnConnectionStatusMessageChanged(string? value)
    {
        HasConnectionStatusMessage = !string.IsNullOrEmpty(value);
    }

    partial void OnIsTestingConnectionChanged(bool value)
    {
        OnPropertyChanged(nameof(IsConnectionTestIdle));
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (!_isInitialized)
        {
            return;
        }

        var appTheme = FromThemeIndex(value);
        _themeService.SetTheme(appTheme);
        _accessibilityService.Announce($"Theme changed to {appTheme}");
    }

    partial void OnNavigationSelectedIndexChanged(int value)
    {
        if (!_isInitialized)
        {
            return;
        }

        var isLeftMode = value == 0;
        _appSettings.IsLeftMode = isLeftMode;
        NavigationOrientationHelper.UpdateNavigationViewForElement(isLeftMode);
    }

    partial void OnListPageSizeChanged(int value)
    {
        if (!_isInitialized || _isUpdatingListPageSize)
        {
            return;
        }

        var normalized = Math.Clamp(value, MinListPageSize, MaxListPageSize);
        if (normalized != value)
        {
            _isUpdatingListPageSize = true;
            ListPageSize = normalized;
            _isUpdatingListPageSize = false;
            return;
        }

        _appSettings.ListPageSize = normalized;
    }

    [RelayCommand]
    private void CopyGitCloneText()
    {
        _clipboardService.SetText(GitCloneText);
    }

    [RelayCommand]
    private void HardReset()
    {
        _connectionStringProvider.ClearUserOverride();
        _appSettings.ResetToDefaults();
        _appLifecycle.Exit();
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _authService.SignOutAsync();
    }

    [RelayCommand]
    private void CancelConnectionDraft()
    {
        _connectionDraft = string.Empty;
        ShowConnectionMaskPlaceholder = _connectionStringProvider.HasUserOverride;
        ConnectionPasswordSurfaceVersion++;
        ConnectionStatusMessage = null;
        ConnectionStatusIsError = false;
    }

    [RelayCommand]
    private void SaveConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionDraft))
        {
            ConnectionStatusMessage = "Введите строку подключения перед сохранением.";
            ConnectionStatusIsError = true;
            return;
        }

        if (!_connectionStringProvider.SetUserOverride(_connectionDraft.Trim()))
        {
            ConnectionStatusMessage = "Не удалось сохранить строку подключения.";
            ConnectionStatusIsError = true;
            return;
        }

        ConnectionStatusMessage = "Строка подключения сохранена в Credential Manager.";
        ConnectionStatusIsError = false;
        _connectionDraft = string.Empty;
        ShowConnectionMaskPlaceholder = true;
        ConnectionPasswordSurfaceVersion++;
        UpdateConnectionSourceDescription();
        _accessibilityService.Announce("Строка подключения к базе сохранена.");
    }

    [RelayCommand]
    private void ClearSavedConnection()
    {
        _connectionStringProvider.ClearUserOverride();
        _connectionDraft = string.Empty;
        ShowConnectionMaskPlaceholder = false;
        ConnectionPasswordSurfaceVersion++;
        UpdateConnectionSourceDescription();
        ConnectionStatusMessage = "Используется строка из файла конфигурации.";
        ConnectionStatusIsError = false;
        _accessibilityService.Announce("Строка подключения сброшена к файлу конфигурации.");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        string connectionToTest;
        if (!string.IsNullOrWhiteSpace(_connectionDraft))
        {
            connectionToTest = _connectionDraft.Trim();
        }
        else
        {
            try
            {
                connectionToTest = _connectionStringProvider.GetEffectiveConnectionString();
            }
            catch (InvalidOperationException)
            {
                ConnectionStatusMessage =
                    "Строка подключения не задана: укажите её в поле или в файле конфигурации.";
                ConnectionStatusIsError = true;
                return;
            }
        }

        IsTestingConnection = true;
        ConnectionStatusMessage = null;
        ConnectionStatusIsError = false;

        try
        {
            var (success, errorMessage) = await _connectionTester.TestAsync(connectionToTest).ConfigureAwait(false);
            await _uiDispatcher.EnqueueAsync(() =>
            {
                IsTestingConnection = false;
                if (success)
                {
                    ConnectionStatusMessage = "Подключение к базе данных успешно.";
                    ConnectionStatusIsError = false;
                }
                else
                {
                    ConnectionStatusMessage = errorMessage ?? "Ошибка подключения.";
                    ConnectionStatusIsError = true;
                }
            });
        }
        catch (Exception ex)
        {
            await _uiDispatcher.EnqueueAsync(() =>
            {
                IsTestingConnection = false;
                ConnectionStatusMessage = ex.Message;
                ConnectionStatusIsError = true;
            });
        }
    }

    private void UpdateConnectionSourceDescription()
    {
        ConnectionSourceDescription = _connectionStringProvider.HasUserOverride
            ? "Источник: сохранённая в Windows строка подключения (Credential Manager)."
            : "Источник: файл конфигурации (ConnectionStrings:DefaultConnection).";
    }

    private static AppTheme FromThemeIndex(int value)
    {
        return value switch
        {
            0 => AppTheme.Light,
            1 => AppTheme.Dark,
            _ => AppTheme.Default
        };
    }

    private static int ToThemeIndex(AppTheme appTheme)
    {
        return appTheme switch
        {
            AppTheme.Light => 0,
            AppTheme.Dark => 1,
            _ => 2
        };
    }
}
