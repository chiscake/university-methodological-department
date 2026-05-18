using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.Bus.Services;

namespace UniversityMethodologicalDepartment.App.Pages.Admin;

public sealed partial class UserManagementPageViewModel : ObservableObject
{
    private readonly AdminUserManagementService _adminUserManagementService;

    public UserManagementPageViewModel(AdminUserManagementService adminUserManagementService)
    {
        _adminUserManagementService = adminUserManagementService;
    }

    public ObservableCollection<UserListItem> Users { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UserListItem? SelectedUser { get; set; }

    [ObservableProperty]
    public partial string CreateEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CreatePassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CreateFullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditFullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCreateMode { get; set; } = true;

    public bool IsEditMode => !IsCreateMode;

    public string FormTitle => IsCreateMode ? "Создание пользователя" : "Редактирование пользователя";

    public Visibility CreateModeVisibility => IsCreateMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EditModeVisibility => IsEditMode ? Visibility.Visible : Visibility.Collapsed;

    partial void OnIsCreateModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(CreateModeVisibility));
        OnPropertyChanged(nameof(EditModeVisibility));
    }

    partial void OnSelectedUserChanged(UserListItem? value)
    {
        IsCreateMode = value is null;
        EditEmail = value?.Email ?? string.Empty;
        EditFullName = value?.FullName ?? string.Empty;
        EditPassword = string.Empty;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var users = await _adminUserManagementService.GetUsersAsync().ConfigureAwait(true);
            Users.Clear();
            foreach (var user in users
                         .Select(u => new UserListItem(
                             u.Id,
                             u.Email,
                             u.FullName ?? "-",
                             ToDisplayRole(u.Role),
                             ToDisplayTime(u.CreatedAtUtc)))
                         .OrderBy(x => x.Email, StringComparer.OrdinalIgnoreCase))
            {
                Users.Add(user);
            }

            if (Users.Count == 0)
            {
                StatusMessage = "Пользователи не найдены.";
            }
        }
        catch (Exception ex)
        {
            Users.Clear();
            StatusMessage = "Ошибка загрузки пользователей.";
            Debug.WriteLine($"[UserManagementPageViewModel] LoadAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreateUserAsync()
    {
        var email = CreateEmail.Trim();
        var password = CreatePassword;
        var fullName = string.IsNullOrWhiteSpace(CreateFullName) ? null : CreateFullName.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            StatusMessage = "Для создания укажите email и password.";
            return;
        }

        IsLoading = true;
        try
        {
            await _adminUserManagementService.CreateUserAsync(email, password, fullName).ConfigureAwait(true);
            CreateEmail = string.Empty;
            CreatePassword = string.Empty;
            CreateFullName = string.Empty;
            StatusMessage = "Пользователь создан.";
            IsCreateMode = true;
            SelectedUser = null;
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка создания пользователя.";
            Debug.WriteLine($"[UserManagementPageViewModel] CreateUserAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task UpdateSelectedUserAsync()
    {
        if (SelectedUser is null)
        {
            StatusMessage = "Выберите пользователя для изменения.";
            return;
        }

        var email = EditEmail.Trim();
        var password = string.IsNullOrWhiteSpace(EditPassword) ? null : EditPassword;
        var fullName = string.IsNullOrWhiteSpace(EditFullName) ? null : EditFullName.Trim();
        var selectedFullName = string.Equals(SelectedUser.FullName, "-", StringComparison.Ordinal) ? null : SelectedUser.FullName;
        var hasFullNameChanged = !string.Equals(fullName, selectedFullName, StringComparison.Ordinal);

        if (string.Equals(email, SelectedUser.Email, StringComparison.OrdinalIgnoreCase) && password is null && !hasFullNameChanged)
        {
            StatusMessage = "Нет изменений для сохранения.";
            return;
        }

        IsLoading = true;
        try
        {
            await _adminUserManagementService.UpdateUserAsync(SelectedUser.Id, email, password, fullName).ConfigureAwait(true);
            EditPassword = string.Empty;
            StatusMessage = "Пользователь обновлён.";
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка обновления пользователя.";
            Debug.WriteLine($"[UserManagementPageViewModel] UpdateSelectedUserAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSelectedUserAsync()
    {
        if (SelectedUser is null)
        {
            StatusMessage = "Выберите пользователя для удаления.";
            return;
        }

        IsLoading = true;
        try
        {
            await _adminUserManagementService.DeleteUserAsync(SelectedUser.Id).ConfigureAwait(true);
            StatusMessage = "Пользователь удалён.";
            SelectedUser = null;
            IsCreateMode = true;
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка удаления пользователя.";
            Debug.WriteLine($"[UserManagementPageViewModel] DeleteSelectedUserAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string ToDisplayTime(string? createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(createdAtUtc))
        {
            return "-";
        }

        if (!DateTimeOffset.TryParse(createdAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return createdAtUtc;
        }

        return parsed.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
    }

    private static string ToDisplayRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return "-";
        }

        return role.Trim().ToLowerInvariant() switch
        {
            "admin" => "администратор",
            "user" => "пользователь",
            _ => role
        };
    }

    [RelayCommand]
    private void SwitchToCreateMode()
    {
        IsCreateMode = true;
        SelectedUser = null;
        EditPassword = string.Empty;
    }
}

public sealed record UserListItem(string Id, string Email, string FullName, string Role, string CreatedAt);
