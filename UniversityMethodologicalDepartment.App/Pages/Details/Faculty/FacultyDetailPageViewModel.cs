using System.Linq;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.Generic;

using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Helpers;
using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.ViewModels.Details.Validation;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.ViewModels.Details;

/// <summary>
/// ViewModel страницы деталей факультета.
/// </summary>
public partial class FacultyDetailPageViewModel : DetailEditorViewModelBase
{
    private readonly IDataService _dataService;
    private readonly IReferenceSearchService _referenceSearchService;

    public FacultyDetailPageViewModel(
        IDataService dataService,
        IReferenceSearchService referenceSearchService,
        IAuthService authService)
        : base(authService)
    {
        _dataService = dataService;
        _referenceSearchService = referenceSearchService;
        TrackEditModelValidity(EditModel);
    }

    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCreateMode { get; set; }

    [ObservableProperty]
    public partial Faculty? Faculty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial FacultyEditModel EditModel { get; set; } = new();

    [ObservableProperty]
    public partial int? SelectedDeanId { get; set; }

    [ObservableProperty]
    public partial string DeanQuery { get; set; } = string.Empty;

    public ObservableCollection<ReferenceSearchItem> DeanSuggestions { get; } = [];

    public sealed partial class FacultyEditModel : ValidatedEditModelBase
    {
        public FacultyEditModel()
        {
            ValidateName(Name);
        }

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        partial void OnNameChanged(string value) => ValidateName(value);

        private void ValidateName(string value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                errors.Add("Название факультета обязательно.");
            }
            else if (trimmed.Length > 200)
            {
                errors.Add("Название не должно превышать 200 символов.");
            }

            SetErrors(nameof(Name), errors);
        }
    }

    /// <summary>Инициализирует данные из параметра навигации.</summary>
    public void Initialize(EntityDetailParameter parameter)
    {
        Id = parameter.Id;
        Title = string.IsNullOrWhiteSpace(parameter.Name) ? "Факультет" : parameter.Name;
        IsCreateMode = parameter.IsCreateMode;
        if (IsCreateMode)
        {
            Faculty = null;
            StatusMessage = string.Empty;
            PrepareEditModel();
        }
    }

    /// <summary>Загружает факультет с деканом и кафедрами из БД.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        Faculty = null;
        try
        {
            var entity = await _dataService.GetFacultyWithDetailsAsync(Id).ConfigureAwait(true);
            Faculty = entity;
            PrepareEditModel();
            StatusMessage = entity is null ? "Не найдено" : string.Empty;
        }
        catch
        {
            StatusMessage = "Ошибка загрузки";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override void PrepareEditModel()
    {
        if (Faculty is null)
        {
            EditModel = new FacultyEditModel();
            SelectedDeanId = null;
            DeanQuery = string.Empty;
            DeanSuggestions.Clear();
            return;
        }

        EditModel = new FacultyEditModel
        {
            Name = Faculty.Name
        };
        SelectedDeanId = Faculty.DeanId;
        DeanQuery = FullName(Faculty.Dean);
        DeanSuggestions.Clear();
    }

    public async Task UpdateDeanSuggestionsAsync(string? query)
    {
        var results = await _referenceSearchService
            .SearchAsync(query ?? string.Empty, 20, ReferenceSearchScope.Employee)
            .ConfigureAwait(true);

        DeanSuggestions.Clear();
        foreach (var item in results)
        {
            DeanSuggestions.Add(item);
        }
    }

    public void SelectDeanSuggestion(ReferenceSearchItem suggestion)
    {
        SelectedDeanId = suggestion.Id;
        DeanQuery = suggestion.Title;
    }

    public void SelectDeanFirstSuggestion()
    {
        if (DeanSuggestions.Count > 0)
        {
            SelectDeanSuggestion(DeanSuggestions[0]);
        }
    }

    protected override async Task<bool> SaveEditorCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var name = EditModel.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            SetEditorError("Название не может быть пустым.");
            return false;
        }

        if (SelectedDeanId is null or <= 0)
        {
            SetEditorError(string.IsNullOrWhiteSpace(DeanQuery)
                ? "Выберите декана."
                : "Значение в поле \"Декан\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (IsCreateMode)
        {
            var dean = await _dataService.GetByIdAsync<Employee>(SelectedDeanId.Value, cancellationToken).ConfigureAwait(true);
            if (dean is null)
            {
                SetEditorError("Невозможно создать факультет: выбранный декан не найден.");
                return false;
            }

            var created = new Faculty
            {
                Name = name,
                DeanId = dean.Id
            };

            var createdOk = await ExecuteMutationAsync(() => _dataService.AddAsync(created, cancellationToken)).ConfigureAwait(true);
            if (!createdOk)
            {
                if (string.IsNullOrWhiteSpace(EditorStatusMessage))
                {
                    SetEditorError("Не удалось создать факультет. Проверьте корректность выбранного декана и повторите попытку.");
                }
                return false;
            }

            var createdEntity = (await _dataService.GetFacultiesAsync(cancellationToken).ConfigureAwait(true))
                .Where(x => string.Equals(x.Name, name, System.StringComparison.Ordinal))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (createdEntity is null)
            {
                SetEditorError("Факультет создан, но не удалось открыть его карточку.");
                return false;
            }

            IsCreateMode = false;
            Id = createdEntity.Id;
            Title = createdEntity.Name;
            await LoadAsync().ConfigureAwait(true);
            StatusMessage = "Запись создана.";
            return true;
        }

        if (Faculty is null)
        {
            SetEditorError("Нет данных для редактирования.");
            return false;
        }

        var updated = new Faculty
        {
            Id = Faculty.Id,
            Name = name,
            DeanId = SelectedDeanId.Value
        };

        var ok = await ExecuteMutationAsync(() => _dataService.UpdateAsync(updated, cancellationToken)).ConfigureAwait(true);
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения. Проверьте, что выбранный декан существует и доступен.");
            }
            return false;
        }

        await LoadAsync().ConfigureAwait(true);
        return true;
    }

    protected override async Task<bool> DeleteEntityCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsCreateMode)
        {
            SetEditorError("Новая запись ещё не создана.");
            return false;
        }

        var ok = await _dataService.DeleteAsync<Faculty>(Id, cancellationToken).ConfigureAwait(true);
        if (!ok)
        {
            SetEditorError("Удаление недоступно.");
            return false;
        }

        Faculty = null;
        StatusMessage = "Запись удалена.";
        return true;
    }

    partial void OnFacultyChanged(Faculty? value)
    {
        NavigateToEmployeeCommand.NotifyCanExecuteChanged();
        NavigateToDepartmentCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditModelChanged(FacultyEditModel value) => TrackEditModelValidity(value);

    [RelayCommand(CanExecute = nameof(CanNavigateEmployee))]
    private void NavigateToEmployee(Employee? employee) => DetailPageNavigator.ToEmployee(employee);

    private static bool CanNavigateEmployee(Employee? employee) => employee is not null;

    [RelayCommand(CanExecute = nameof(CanNavigateDepartment))]
    private void NavigateToDepartment(Department? department) => DetailPageNavigator.ToDepartment(department);

    private static bool CanNavigateDepartment(Department? department) => department is not null;

    private static string FullName(Employee? employee)
    {
        if (employee is null)
        {
            return string.Empty;
        }

        var parts = new[] { employee.Surname, employee.Name, employee.Patronymic ?? string.Empty };
        return string.Join(' ', parts.Where(static part => !string.IsNullOrWhiteSpace(part))).Trim();
    }
}
