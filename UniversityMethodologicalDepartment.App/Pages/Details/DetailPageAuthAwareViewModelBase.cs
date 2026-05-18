using System;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.ViewModels.Details;

/// <summary>
/// Базовый ViewModel для detail-страниц с отслеживанием статуса авторизации.
/// Предоставляет флаги, которые можно напрямую использовать в XAML бокового редактора.
/// </summary>
public abstract partial class DetailPageAuthAwareViewModelBase : ObservableObject, IDisposable
{
    private readonly IAuthService _authService;
    private bool _isDisposed;

    protected DetailPageAuthAwareViewModelBase(IAuthService authService)
    {
        _authService = authService;
        IsAuthenticated = _authService.IsAuthenticated;
        _authService.AuthStateChanged += OnAuthStateChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditorReadOnly))]
    [NotifyPropertyChangedFor(nameof(CanAddOrDelete))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    public partial bool IsAuthenticated { get; set; }

    public bool IsEditorReadOnly => !IsAuthenticated;

    public bool CanAddOrDelete => IsAuthenticated;

    public bool CanEdit => IsAuthenticated;

    private void OnAuthStateChanged(object? sender, EventArgs e)
    {
        IsAuthenticated = _authService.IsAuthenticated;
        OnAuthenticationChanged(IsAuthenticated);
    }

    /// <summary>
    /// Хук для производных классов при изменении состояния аутентификации.
    /// </summary>
    protected virtual void OnAuthenticationChanged(bool isAuthenticated)
    {
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _authService.AuthStateChanged -= OnAuthStateChanged;
        GC.SuppressFinalize(this);
    }
}
