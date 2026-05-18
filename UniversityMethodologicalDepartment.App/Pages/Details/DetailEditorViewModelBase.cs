using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Services.Errors;

namespace UniversityMethodologicalDepartment.App.ViewModels.Details;

/// <summary>
/// Базовый класс поведения бокового редактора на detail-страницах.
/// Содержит логику открытия/закрытия, сохранения и удаления.
/// </summary>
public abstract partial class DetailEditorViewModelBase : DetailPageAuthAwareViewModelBase
{
    private static readonly Brush ErrorBrush = new SolidColorBrush(Colors.Red);
    private INotifyDataErrorInfo? _trackedEditModel;

    protected DetailEditorViewModelBase(IAuthService authService)
        : base(authService)
    {
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowEditor))]
    [NotifyPropertyChangedFor(nameof(CanSaveEditor))]
    [NotifyCanExecuteChangedFor(nameof(CloseEditorCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditorCommand))]
    public partial bool IsEditorOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveEditor))]
    [NotifyPropertyChangedFor(nameof(CanDeleteEntity))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditorCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteEntityCommand))]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveEditor))]
    [NotifyPropertyChangedFor(nameof(CanDeleteEntity))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditorCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteEntityCommand))]
    public partial bool IsDeleting { get; set; }

    [ObservableProperty]
    public partial string EditorStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditorError))]
    [NotifyPropertyChangedFor(nameof(EditorStatusForeground))]
    public partial EditorStatusSeverity EditorMessageSeverity { get; set; }

    public bool IsEditorError => EditorMessageSeverity == EditorStatusSeverity.Error;

    public Brush EditorStatusForeground =>
        IsEditorError
            ? ErrorBrush
            : (Brush?)Application.Current.Resources["TextFillColorSecondaryBrush"] ?? ErrorBrush;

    public bool CanShowEditor => IsAuthenticated && IsEditorOpen;

    public bool CanShowActions => IsAuthenticated;

    public bool CanSaveEditor =>
        IsAuthenticated && IsEditorOpen && !IsSaving && !IsDeleting
        && !(_trackedEditModel?.HasErrors ?? false);

    public bool CanDeleteEntity => IsAuthenticated && !IsSaving && !IsDeleting;

    /// <summary>
    /// Подписаться на <see cref="INotifyDataErrorInfo.ErrorsChanged"/> текущей edit-модели,
    /// чтобы кнопка Сохранить блокировалась при наличии ошибок валидации.
    /// Производный VM должен вызывать этот метод в <c>OnEditModelChanged</c>.
    /// </summary>
    protected void TrackEditModelValidity(INotifyDataErrorInfo? editModel)
    {
        if (ReferenceEquals(_trackedEditModel, editModel))
        {
            return;
        }

        if (_trackedEditModel is not null)
        {
            _trackedEditModel.ErrorsChanged -= OnEditModelErrorsChanged;
        }

        _trackedEditModel = editModel;

        if (_trackedEditModel is not null)
        {
            _trackedEditModel.ErrorsChanged += OnEditModelErrorsChanged;
        }

        OnPropertyChanged(nameof(CanSaveEditor));
        SaveEditorCommand.NotifyCanExecuteChanged();
    }

    private void OnEditModelErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanSaveEditor));
        SaveEditorCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanShowActions))]
    private void OpenEditor()
    {
        PrepareEditModel();
        ClearEditorStatus();
        IsEditorOpen = true;
    }

    [RelayCommand(CanExecute = nameof(IsEditorOpen))]
    private void CloseEditor()
    {
        IsEditorOpen = false;
        ClearEditorStatus();
    }

    [RelayCommand(CanExecute = nameof(CanSaveEditor))]
    private async Task SaveEditorAsync()
    {
        if (!CanSaveEditor)
        {
            return;
        }

        IsSaving = true;
        ClearEditorStatus();
        try
        {
            var saved = await SaveEditorCoreAsync(CancellationToken.None).ConfigureAwait(true);
            if (saved)
            {
                IsEditorOpen = false;
            }
            else if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения.");
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteEntity))]
    private async Task DeleteEntityAsync()
    {
        if (!CanDeleteEntity)
        {
            return;
        }

        IsDeleting = true;
        ClearEditorStatus();
        try
        {
            var deleted = await DeleteEntityCoreAsync(CancellationToken.None).ConfigureAwait(true);
            if (deleted)
            {
                IsEditorOpen = false;
            }
            else if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось удалить запись.");
            }
        }
        finally
        {
            IsDeleting = false;
        }
    }

    protected override void OnAuthenticationChanged(bool isAuthenticated)
    {
        base.OnAuthenticationChanged(isAuthenticated);
        if (!isAuthenticated)
        {
            IsEditorOpen = false;
        }

        OnPropertyChanged(nameof(CanShowEditor));
        OnPropertyChanged(nameof(CanShowActions));
        OnPropertyChanged(nameof(CanSaveEditor));
        OnPropertyChanged(nameof(CanDeleteEntity));
        OpenEditorCommand.NotifyCanExecuteChanged();
        CloseEditorCommand.NotifyCanExecuteChanged();
        SaveEditorCommand.NotifyCanExecuteChanged();
        DeleteEntityCommand.NotifyCanExecuteChanged();
    }

    protected abstract void PrepareEditModel();

    protected abstract Task<bool> SaveEditorCoreAsync(CancellationToken cancellationToken);

    protected abstract Task<bool> DeleteEntityCoreAsync(CancellationToken cancellationToken);

    protected void SetEditorError(string message)
    {
        EditorStatusMessage = message;
        EditorMessageSeverity = EditorStatusSeverity.Error;
    }

    /// <summary>
    /// Запускает мутацию слоя данных с единообразной обработкой распознанных бизнес-ошибок БД:
    /// при <see cref="DatabaseBusinessException"/> отображает её сообщение через <see cref="SetEditorError"/>
    /// и возвращает <c>false</c>; остальные исключения пробрасываются без изменений, успешный результат
    /// возвращается как есть.
    /// </summary>
    protected async Task<bool> ExecuteMutationAsync(Func<Task<bool>> action)
    {
        try
        {
            return await action().ConfigureAwait(true);
        }
        catch (DatabaseBusinessException ex)
        {
            SetEditorError(ex.Message);
            return false;
        }
    }

    protected void SetEditorInfo(string message)
    {
        EditorStatusMessage = message;
        EditorMessageSeverity = EditorStatusSeverity.Info;
    }

    protected void ClearEditorStatus()
    {
        EditorStatusMessage = string.Empty;
        EditorMessageSeverity = EditorStatusSeverity.None;
    }
}

public enum EditorStatusSeverity
{
    None = 0,
    Info = 1,
    Error = 2
}
