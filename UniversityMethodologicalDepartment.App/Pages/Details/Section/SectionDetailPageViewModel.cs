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
/// ViewModel страницы деталей секции.
/// </summary>
public partial class SectionDetailPageViewModel : DetailEditorViewModelBase
{
    private readonly IDataService _dataService;
    private readonly IReferenceSearchService _referenceSearchService;

    public SectionDetailPageViewModel(
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
    public partial Section? Section { get; set; }

    public bool HasHead => Section?.Head is not null;

    public bool IsHeadMissing => Section is not null && Section.Head is null;

    public string HeadDisplayLine => ComposeHeadLine(Section?.Head);

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SectionEditModel EditModel { get; set; } = new();

    [ObservableProperty]
    public partial int? SelectedDepartmentId { get; set; }

    [ObservableProperty]
    public partial string DepartmentQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? SelectedHeadId { get; set; }

    [ObservableProperty]
    public partial string HeadQuery { get; set; } = string.Empty;

    public ObservableCollection<ReferenceSearchItem> DepartmentSuggestions { get; } = [];

    public ObservableCollection<ReferenceSearchItem> HeadSuggestions { get; } = [];

    public sealed partial class SectionEditModel : ValidatedEditModelBase
    {
        public SectionEditModel()
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
                errors.Add("Название секции обязательно.");
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

    partial void OnSectionChanged(Section? value)
    {
        NavigateToEmployeeCommand.NotifyCanExecuteChanged();
        NavigateToDepartmentCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasHead));
        OnPropertyChanged(nameof(IsHeadMissing));
        OnPropertyChanged(nameof(HeadDisplayLine));
    }

    partial void OnEditModelChanged(SectionEditModel value) => TrackEditModelValidity(value);

    /// <summary>Инициализирует данные из параметра навигации.</summary>
    public void Initialize(EntityDetailParameter parameter)
    {
        Id = parameter.Id;
        Title = string.IsNullOrWhiteSpace(parameter.Name) ? "Секция" : parameter.Name;
        IsCreateMode = parameter.IsCreateMode;
        if (IsCreateMode)
        {
            Section = null;
            StatusMessage = string.Empty;
            PrepareEditModel();
        }
    }

    /// <summary>Загружает секцию с кафедрой, руководителем и сотрудниками из БД.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        Section = null;
        try
        {
            var entity = await _dataService.GetSectionWithDetailsAsync(Id).ConfigureAwait(true);
            Section = entity;
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
        if (Section is null)
        {
            EditModel = new SectionEditModel();
            SelectedDepartmentId = null;
            DepartmentQuery = string.Empty;
            SelectedHeadId = null;
            HeadQuery = string.Empty;
            DepartmentSuggestions.Clear();
            HeadSuggestions.Clear();
            return;
        }

        EditModel = new SectionEditModel
        {
            Name = Section.Name,
            Phones = Section.Phones
        };
        SelectedDepartmentId = Section.DepartmentId;
        DepartmentQuery = Section.Department?.Name ?? string.Empty;
        SelectedHeadId = Section.HeadId;
        HeadQuery = FullName(Section.Head);
        DepartmentSuggestions.Clear();
        HeadSuggestions.Clear();
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

        if (SelectedDepartmentId is null or <= 0)
        {
            SetEditorError(string.IsNullOrWhiteSpace(DepartmentQuery)
                ? "Выберите кафедру."
                : "Значение в поле \"Кафедра\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(HeadQuery) && (SelectedHeadId is null or <= 0))
        {
            SetEditorError("Значение в поле \"Руководитель секции\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (IsCreateMode)
        {
            var department = await _dataService.GetByIdAsync<Department>(SelectedDepartmentId.Value, cancellationToken).ConfigureAwait(true);
            if (department is null)
            {
                SetEditorError("Невозможно создать секцию: выбранная кафедра не найдена.");
                return false;
            }

            var created = new Section
            {
                Name = name,
                Phones = string.IsNullOrWhiteSpace(EditModel.Phones) ? null : EditModel.Phones.Trim(),
                DepartmentId = department.Id,
                HeadId = SelectedHeadId
            };

            var createdOk = await ExecuteMutationAsync(() => _dataService.AddAsync(created, cancellationToken)).ConfigureAwait(true);
            if (!createdOk)
            {
                if (string.IsNullOrWhiteSpace(EditorStatusMessage))
                {
                    SetEditorError("Не удалось создать секцию. Проверьте корректность выбранных связанных данных и повторите попытку.");
                }
                return false;
            }

            var createdEntity = (await _dataService.GetSectionsAsync(department.Id, cancellationToken).ConfigureAwait(true))
                .Where(x => string.Equals(x.Name, name, System.StringComparison.Ordinal))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (createdEntity is null)
            {
                SetEditorError("Секция создана, но не удалось открыть её карточку.");
                return false;
            }

            IsCreateMode = false;
            Id = createdEntity.Id;
            Title = createdEntity.Name;
            await LoadAsync().ConfigureAwait(true);
            StatusMessage = "Запись создана.";
            return true;
        }

        if (Section is null)
        {
            SetEditorError("Нет данных для редактирования.");
            return false;
        }

        var updated = new Section
        {
            Id = Section.Id,
            Name = name,
            Phones = string.IsNullOrWhiteSpace(EditModel.Phones) ? null : EditModel.Phones.Trim(),
            DepartmentId = SelectedDepartmentId.Value,
            HeadId = SelectedHeadId
        };

        var ok = await ExecuteMutationAsync(() => _dataService.UpdateAsync(updated, cancellationToken)).ConfigureAwait(true);
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения. Проверьте, что выбранные кафедра и руководитель секции существуют и доступны.");
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

        var ok = await _dataService.DeleteAsync<Section>(Id, cancellationToken).ConfigureAwait(true);
        if (!ok)
        {
            SetEditorError("Удаление недоступно.");
            return false;
        }

        Section = null;
        StatusMessage = "Запись удалена.";
        return true;
    }

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
