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
/// ViewModel страницы деталей кафедры.
/// </summary>
public partial class DepartmentDetailPageViewModel : DetailEditorViewModelBase
{
    private readonly IDataService _dataService;
    private readonly IReferenceSearchService _referenceSearchService;

    public DepartmentDetailPageViewModel(
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
    public partial Department? Department { get; set; }

    public bool HasHead => Department?.Head is not null;

    public bool IsHeadMissing => Department is not null && Department.Head is null;

    public string HeadDisplayLine => ComposeHeadLine(Department?.Head);

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DepartmentEditModel EditModel { get; set; } = new();

    [ObservableProperty]
    public partial Faculty? SelectedFaculty { get; set; }

    [ObservableProperty]
    public partial int? SelectedHeadId { get; set; }

    [ObservableProperty]
    public partial string HeadQuery { get; set; } = string.Empty;

    public ObservableCollection<Faculty> Faculties { get; } = [];

    public ObservableCollection<ReferenceSearchItem> HeadSuggestions { get; } = [];

    public sealed partial class DepartmentEditModel : ValidatedEditModelBase
    {
        public DepartmentEditModel()
        {
            ValidateName(Name);
            ValidatePhones(Phones);
        }

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Phones { get; set; }

        partial void OnNameChanged(string value) => ValidateName(value);

        partial void OnPhonesChanged(string? value) => ValidatePhones(value);

        private void ValidateName(string value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                errors.Add("Название кафедры обязательно.");
            }
            else if (trimmed.Length > 200)
            {
                errors.Add("Название не должно превышать 200 символов.");
            }

            SetErrors(nameof(Name), errors);
        }

        private void ValidatePhones(string? value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim();

            if (!string.IsNullOrEmpty(trimmed))
            {
                if (trimmed.Length > 200)
                {
                    errors.Add("Телефоны не должны превышать 200 символов.");
                }
                else if (!ValidationPatterns.PhonesRegex().IsMatch(trimmed))
                {
                    errors.Add("Допустимы только цифры и символы + - ( ) , пробел.");
                }
            }

            SetErrors(nameof(Phones), errors);
        }
    }

    /// <summary>Инициализирует данные из параметра навигации.</summary>
    public void Initialize(EntityDetailParameter parameter)
    {
        Id = parameter.Id;
        Title = string.IsNullOrWhiteSpace(parameter.Name) ? "Кафедра" : parameter.Name;
        IsCreateMode = parameter.IsCreateMode;
        if (IsCreateMode)
        {
            Department = null;
            StatusMessage = string.Empty;
            _ = EnsureFacultiesLoadedAsync();
            PrepareEditModel();
        }
    }

    /// <summary>Загружает кафедру с факультетом, заведующим, сотрудниками, дисциплинами и секциями из БД.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        Department = null;
        try
        {
            var entity = await _dataService.GetDepartmentWithDetailsAsync(Id).ConfigureAwait(true);
            Department = entity;
            await EnsureFacultiesLoadedAsync().ConfigureAwait(true);
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
        if (Department is null)
        {
            EditModel = new DepartmentEditModel();
            SelectedFaculty = Faculties.FirstOrDefault();
            SelectedHeadId = null;
            HeadQuery = string.Empty;
            HeadSuggestions.Clear();
            return;
        }

        EditModel = new DepartmentEditModel
        {
            Name = Department.Name,
            Phones = Department.Phones
        };
        SelectedFaculty = Faculties.FirstOrDefault(x => x.Id == Department.FacultyId)
            ?? Department.Faculty;
        SelectedHeadId = Department.HeadId;
        HeadQuery = FullName(Department.Head);
        HeadSuggestions.Clear();
    }

    public async Task UpdateHeadSuggestionsAsync(string? query)
    {
        var results = await _referenceSearchService
            .SearchAsync(query ?? string.Empty, 20, ReferenceSearchScope.Employee)
            .ConfigureAwait(true);

        HeadSuggestions.Clear();
        foreach (var item in results)
        {
            HeadSuggestions.Add(item);
        }
    }

    public void SelectHeadSuggestion(ReferenceSearchItem suggestion)
    {
        SelectedHeadId = suggestion.Id;
        HeadQuery = suggestion.Title;
    }

    public void SelectHeadFirstSuggestion()
    {
        if (HeadSuggestions.Count > 0)
        {
            SelectHeadSuggestion(HeadSuggestions[0]);
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

        if (SelectedFaculty is null)
        {
            SetEditorError("Выберите факультет.");
            return false;
        }

        if (SelectedHeadId is null or <= 0)
        {
            SetEditorError(string.IsNullOrWhiteSpace(HeadQuery)
                ? "Выберите заведующего."
                : "Значение в поле \"Заведующий\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (IsCreateMode)
        {
            var head = await _dataService.GetByIdAsync<Employee>(SelectedHeadId.Value, cancellationToken).ConfigureAwait(true);
            if (head is null)
            {
                SetEditorError("Невозможно создать кафедру: выбранный заведующий не найден.");
                return false;
            }

            var created = new Department
            {
                Name = name,
                Phones = string.IsNullOrWhiteSpace(EditModel.Phones) ? null : EditModel.Phones.Trim(),
                FacultyId = SelectedFaculty.Id,
                HeadId = head.Id
            };

            var createdOk = await ExecuteMutationAsync(() => _dataService.AddAsync(created, cancellationToken)).ConfigureAwait(true);
            if (!createdOk)
            {
                if (string.IsNullOrWhiteSpace(EditorStatusMessage))
                {
                    SetEditorError("Не удалось создать кафедру. Проверьте корректность выбранных связанных данных и повторите попытку.");
                }
                return false;
            }

            var createdEntity = (await _dataService.GetDepartmentsAsync(SelectedFaculty.Id, cancellationToken).ConfigureAwait(true))
                .Where(x => string.Equals(x.Name, name, System.StringComparison.Ordinal))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (createdEntity is null)
            {
                SetEditorError("Кафедра создана, но не удалось открыть её карточку.");
                return false;
            }

            IsCreateMode = false;
            Id = createdEntity.Id;
            Title = createdEntity.Name;
            await LoadAsync().ConfigureAwait(true);
            StatusMessage = "Запись создана.";
            return true;
        }

        if (Department is null)
        {
            SetEditorError("Нет данных для редактирования.");
            return false;
        }

        var updated = new Department
        {
            Id = Department.Id,
            Name = name,
            Phones = string.IsNullOrWhiteSpace(EditModel.Phones) ? null : EditModel.Phones.Trim(),
            FacultyId = SelectedFaculty.Id,
            HeadId = SelectedHeadId.Value
        };

        var ok = await ExecuteMutationAsync(() => _dataService.UpdateAsync(updated, cancellationToken)).ConfigureAwait(true);
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения. Проверьте, что выбранные факультет и заведующий существуют и доступны.");
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

        var ok = await _dataService.DeleteAsync<Department>(Id, cancellationToken).ConfigureAwait(true);
        if (!ok)
        {
            SetEditorError("Удаление недоступно.");
            return false;
        }

        Department = null;
        StatusMessage = "Запись удалена.";
        return true;
    }

    partial void OnDepartmentChanged(Department? value)
    {
        NavigateToFacultyCommand.NotifyCanExecuteChanged();
        NavigateToEmployeeCommand.NotifyCanExecuteChanged();
        NavigateToDisciplineCommand.NotifyCanExecuteChanged();
        NavigateToSectionCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasHead));
        OnPropertyChanged(nameof(IsHeadMissing));
        OnPropertyChanged(nameof(HeadDisplayLine));
    }

    partial void OnEditModelChanged(DepartmentEditModel value) => TrackEditModelValidity(value);

    [RelayCommand(CanExecute = nameof(CanNavigateFaculty))]
    private void NavigateToFaculty(Faculty? faculty) => DetailPageNavigator.ToFaculty(faculty);

    private static bool CanNavigateFaculty(Faculty? faculty) => faculty is not null;

    [RelayCommand(CanExecute = nameof(CanNavigateEmployee))]
    private void NavigateToEmployee(Employee? employee) => DetailPageNavigator.ToEmployee(employee);

    private static bool CanNavigateEmployee(Employee? employee) => employee is not null;

    [RelayCommand(CanExecute = nameof(CanNavigateDiscipline))]
    private void NavigateToDiscipline(Discipline? discipline) => DetailPageNavigator.ToDiscipline(discipline);

    private static bool CanNavigateDiscipline(Discipline? discipline) => discipline is not null;

    [RelayCommand(CanExecute = nameof(CanNavigateSection))]
    private void NavigateToSection(Section? section) => DetailPageNavigator.ToSection(section);

    private static bool CanNavigateSection(Section? section) => section is not null;

    private async Task EnsureFacultiesLoadedAsync()
    {
        if (Faculties.Count > 0)
        {
            return;
        }

        var list = await _dataService.GetFacultiesAsync().ConfigureAwait(true);
        Faculties.Clear();
        foreach (var faculty in list.OrderBy(x => x.Name))
        {
            Faculties.Add(faculty);
        }
    }

    private static string FullName(Employee? employee)
    {
        if (employee is null)
        {
            return string.Empty;
        }

        var parts = new[] { employee.Surname, employee.Name, employee.Patronymic ?? string.Empty };
        return string.Join(' ', parts.Where(static part => !string.IsNullOrWhiteSpace(part))).Trim();
    }

    private static string ComposeHeadLine(Employee? employee)
    {
        if (employee is null)
        {
            return string.Empty;
        }

        var headDetails = new[] { employee.Title, employee.Degree }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        var fullName = FullName(employee);
        if (headDetails.Length == 0)
        {
            return fullName;
        }

        return $"{fullName}, {string.Join(", ", headDetails)}";
    }
}

