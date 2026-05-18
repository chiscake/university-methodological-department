using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

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
/// ViewModel страницы деталей дисциплины.
/// </summary>
public partial class DisciplineDetailPageViewModel : DetailEditorViewModelBase
{
    private readonly IDataService _dataService;
    private readonly IReferenceSearchService _referenceSearchService;

    public DisciplineDetailPageViewModel(
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
    public partial Discipline? Discipline { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DisciplineEditModel EditModel { get; set; } = new();

    [ObservableProperty]
    public partial int? SelectedDepartmentId { get; set; }

    [ObservableProperty]
    public partial string DepartmentQuery { get; set; } = string.Empty;

    public ObservableCollection<ReferenceSearchItem> DepartmentSuggestions { get; } = [];

    public sealed partial class DisciplineEditModel : ValidatedEditModelBase
    {
        public DisciplineEditModel()
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
                errors.Add("Название дисциплины обязательно.");
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
        Title = string.IsNullOrWhiteSpace(parameter.Name) ? "Дисциплина" : parameter.Name;
        IsCreateMode = parameter.IsCreateMode;
        if (IsCreateMode)
        {
            Discipline = null;
            StatusMessage = string.Empty;
            PrepareEditModel();
        }
    }

    /// <summary>Загружает дисциплину с кафедрой и элементами учебного плана из БД.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        Discipline = null;
        try
        {
            var entity = await _dataService.GetDisciplineWithDetailsAsync(Id).ConfigureAwait(true);
            Discipline = entity;
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
        if (Discipline is null)
        {
            EditModel = new DisciplineEditModel();
            SelectedDepartmentId = null;
            DepartmentQuery = string.Empty;
            DepartmentSuggestions.Clear();
            return;
        }

        EditModel = new DisciplineEditModel
        {
            Name = Discipline.Name
        };
        SelectedDepartmentId = Discipline.DepartmentId;
        DepartmentQuery = Discipline.Department?.Name ?? string.Empty;
        DepartmentSuggestions.Clear();
    }

    public async Task UpdateDepartmentSuggestionsAsync(string? query)
    {
        var results = await _referenceSearchService
            .SearchAsync(query ?? string.Empty, 20, ReferenceSearchScope.Department)
            .ConfigureAwait(true);

        DepartmentSuggestions.Clear();
        foreach (var item in results)
        {
            DepartmentSuggestions.Add(item);
        }
    }

    public void SelectDepartmentSuggestion(ReferenceSearchItem suggestion)
    {
        SelectedDepartmentId = suggestion.Id;
        DepartmentQuery = suggestion.Title;
    }

    public void SelectDepartmentFirstSuggestion()
    {
        if (DepartmentSuggestions.Count > 0)
        {
            SelectDepartmentSuggestion(DepartmentSuggestions[0]);
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

        if (SelectedDepartmentId is null or <= 0)
        {
            SetEditorError(string.IsNullOrWhiteSpace(DepartmentQuery)
                ? "Выберите кафедру."
                : "Значение в поле \"Кафедра\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (IsCreateMode)
        {
            var department = await _dataService.GetByIdAsync<Department>(SelectedDepartmentId.Value, cancellationToken).ConfigureAwait(true);
            if (department is null)
            {
                SetEditorError("Невозможно создать дисциплину: выбранная кафедра не найдена.");
                return false;
            }

            var created = new Discipline
            {
                Name = name,
                DepartmentId = department.Id
            };

            var createdOk = await ExecuteMutationAsync(() => _dataService.AddAsync(created, cancellationToken)).ConfigureAwait(true);
            if (!createdOk)
            {
                if (string.IsNullOrWhiteSpace(EditorStatusMessage))
                {
                    SetEditorError("Не удалось создать дисциплину. Проверьте корректность выбранной кафедры и повторите попытку.");
                }
                return false;
            }

            var createdEntity = (await _dataService.GetDisciplinesAsync(department.Id, cancellationToken).ConfigureAwait(true))
                .Where(x => string.Equals(x.Name, name, System.StringComparison.Ordinal))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (createdEntity is null)
            {
                SetEditorError("Дисциплина создана, но не удалось открыть её карточку.");
                return false;
            }

            IsCreateMode = false;
            Id = createdEntity.Id;
            Title = createdEntity.Name;
            await LoadAsync().ConfigureAwait(true);
            StatusMessage = "Запись создана.";
            return true;
        }

        if (Discipline is null)
        {
            SetEditorError("Нет данных для редактирования.");
            return false;
        }

        var updated = new Discipline
        {
            Id = Discipline.Id,
            Name = name,
            DepartmentId = SelectedDepartmentId.Value
        };

        var ok = await ExecuteMutationAsync(() => _dataService.UpdateAsync(updated, cancellationToken)).ConfigureAwait(true);
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения. Проверьте, что выбранная кафедра существует и доступна.");
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

        var ok = await _dataService.DeleteAsync<Discipline>(Id, cancellationToken).ConfigureAwait(true);
        if (!ok)
        {
            SetEditorError("Удаление недоступно.");
            return false;
        }

        Discipline = null;
        StatusMessage = "Запись удалена.";
        return true;
    }

    partial void OnDisciplineChanged(Discipline? value)
    {
        NavigateToDepartmentCommand.NotifyCanExecuteChanged();
        NavigateToCurriculumItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditModelChanged(DisciplineEditModel value) => TrackEditModelValidity(value);

    [RelayCommand(CanExecute = nameof(CanNavigateDepartment))]
    private void NavigateToDepartment(Department? department) => DetailPageNavigator.ToDepartment(department);

    private static bool CanNavigateDepartment(Department? department) => department is not null;

    [RelayCommand(CanExecute = nameof(CanNavigateCurriculumItem))]
    private void NavigateToCurriculumItem(CurriculumItem? item) => DetailPageNavigator.ToCurriculumItem(item);

    private static bool CanNavigateCurriculumItem(CurriculumItem? item) => item is not null;
}
