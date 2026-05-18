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
/// ViewModel страницы деталей сотрудника.
/// </summary>
public partial class EmployeeDetailPageViewModel : DetailEditorViewModelBase
{
    private readonly IDataService _dataService;
    private readonly IReferenceSearchService _referenceSearchService;

    public EmployeeDetailPageViewModel(
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
    public partial Employee? Employee { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial EmployeeEditModel EditModel { get; set; } = new();

    [ObservableProperty]
    public partial int? SelectedDepartmentId { get; set; }

    [ObservableProperty]
    public partial string DepartmentQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? SelectedSectionId { get; set; }

    [ObservableProperty]
    public partial string SectionQuery { get; set; } = string.Empty;

    public ObservableCollection<ReferenceSearchItem> DepartmentSuggestions { get; } = [];

    public ObservableCollection<ReferenceSearchItem> SectionSuggestions { get; } = [];

    public sealed partial class EmployeeEditModel : ValidatedEditModelBase
    {
        public EmployeeEditModel()
        {
            ValidateSurname(Surname);
            ValidateName(Name);
            ValidatePatronymic(Patronymic);
            ValidateDegree(Degree);
            ValidateTitle(Title);
            ValidatePhone(Phone);
            ValidateEmail(Email);
        }

        [ObservableProperty]
        public partial string Surname { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Patronymic { get; set; }

        [ObservableProperty]
        public partial string? Degree { get; set; }

        [ObservableProperty]
        public partial string? Title { get; set; }

        [ObservableProperty]
        public partial string? Phone { get; set; }

        [ObservableProperty]
        public partial string? Email { get; set; }

        partial void OnSurnameChanged(string value) => ValidateSurname(value);

        partial void OnNameChanged(string value) => ValidateName(value);

        partial void OnPatronymicChanged(string? value) => ValidatePatronymic(value);

        partial void OnDegreeChanged(string? value) => ValidateDegree(value);

        partial void OnTitleChanged(string? value) => ValidateTitle(value);

        partial void OnPhoneChanged(string? value) => ValidatePhone(value);

        partial void OnEmailChanged(string? value) => ValidateEmail(value);

        private void ValidateSurname(string value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                errors.Add("Фамилия обязательна.");
            }
            else if (trimmed.Length > 100)
            {
                errors.Add("Фамилия не должна превышать 100 символов.");
            }

            SetErrors(nameof(Surname), errors);
        }

        private void ValidateName(string value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                errors.Add("Имя обязательно.");
            }
            else if (trimmed.Length > 100)
            {
                errors.Add("Имя не должно превышать 100 символов.");
            }

            SetErrors(nameof(Name), errors);
        }

        private void ValidatePatronymic(string? value) =>
            ValidateOptionalLength(nameof(Patronymic), value, 100, "Отчество");

        private void ValidateDegree(string? value) =>
            ValidateOptionalLength(nameof(Degree), value, 100, "Степень");

        private void ValidateTitle(string? value) =>
            ValidateOptionalLength(nameof(Title), value, 100, "Звание");

        private void ValidateOptionalLength(string propertyName, string? value, int max, string fieldLabel)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim();

            if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > max)
            {
                errors.Add($"{fieldLabel} не должно превышать {max} символов.");
            }

            SetErrors(propertyName, errors);
        }

        private void ValidatePhone(string? value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim();

            if (!string.IsNullOrEmpty(trimmed))
            {
                if (trimmed.Length > 32)
                {
                    errors.Add("Телефон не должен превышать 32 символа.");
                }
                else if (!ValidationPatterns.PhonesRegex().IsMatch(trimmed))
                {
                    errors.Add("Допустимы только цифры и символы + - ( ) , пробел.");
                }
            }

            SetErrors(nameof(Phone), errors);
        }

        private void ValidateEmail(string? value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim();

            if (!string.IsNullOrEmpty(trimmed))
            {
                if (trimmed.Length > 200)
                {
                    errors.Add("Email не должен превышать 200 символов.");
                }
                else if (!ValidationPatterns.EmailRegex().IsMatch(trimmed))
                {
                    errors.Add("Введите корректный email вида name@example.com.");
                }
            }

            SetErrors(nameof(Email), errors);
        }
    }

    /// <summary>Инициализирует данные из параметра навигации.</summary>
    public void Initialize(EntityDetailParameter parameter)
    {
        Id = parameter.Id;
        Title = string.IsNullOrWhiteSpace(parameter.Name) ? "Сотрудник" : parameter.Name;
        IsCreateMode = parameter.IsCreateMode;
        if (IsCreateMode)
        {
            Employee = null;
            StatusMessage = string.Empty;
            PrepareEditModel();
        }
    }

    /// <summary>Загружает сотрудника с кафедрой и секцией из БД.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        Employee = null;
        try
        {
            var entity = await _dataService.GetEmployeeWithDetailsAsync(Id).ConfigureAwait(true);
            Employee = entity;
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
        if (Employee is null)
        {
            EditModel = new EmployeeEditModel();
            SelectedDepartmentId = null;
            DepartmentQuery = string.Empty;
            SelectedSectionId = null;
            SectionQuery = string.Empty;
            DepartmentSuggestions.Clear();
            SectionSuggestions.Clear();
            return;
        }

        EditModel = new EmployeeEditModel
        {
            Surname = Employee.Surname,
            Name = Employee.Name,
            Patronymic = Employee.Patronymic,
            Degree = Employee.Degree,
            Title = Employee.Title,
            Phone = Employee.Phone,
            Email = Employee.Email
        };
        SelectedDepartmentId = Employee.DepartmentId;
        DepartmentQuery = Employee.Department?.Name ?? string.Empty;
        SelectedSectionId = Employee.SectionId;
        SectionQuery = Employee.Section?.Name ?? string.Empty;
        DepartmentSuggestions.Clear();
        SectionSuggestions.Clear();
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

    public async Task UpdateSectionSuggestionsAsync(string? query)
    {
        var results = await _referenceSearchService
            .SearchAsync(query ?? string.Empty, 20, ReferenceSearchScope.Section)
            .ConfigureAwait(true);

        SectionSuggestions.Clear();
        foreach (var item in results)
        {
            SectionSuggestions.Add(item);
        }
    }

    public void SelectSectionSuggestion(ReferenceSearchItem suggestion)
    {
        SelectedSectionId = suggestion.Id;
        SectionQuery = suggestion.Title;
    }

    public void SelectSectionFirstSuggestion()
    {
        if (SectionSuggestions.Count > 0)
        {
            SelectSectionSuggestion(SectionSuggestions[0]);
        }
    }

    protected override async Task<bool> SaveEditorCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var surname = EditModel.Surname?.Trim() ?? string.Empty;
        var name = EditModel.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(surname) || string.IsNullOrWhiteSpace(name))
        {
            SetEditorError("Имя и фамилия обязательны.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(DepartmentQuery) && (SelectedDepartmentId is null or <= 0))
        {
            SetEditorError("Значение в поле \"Кафедра\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SectionQuery) && (SelectedSectionId is null or <= 0))
        {
            SetEditorError("Значение в поле \"Секция\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (IsCreateMode)
        {
            var created = new Employee
            {
                Surname = surname,
                Name = name,
                Patronymic = string.IsNullOrWhiteSpace(EditModel.Patronymic) ? null : EditModel.Patronymic.Trim(),
                Degree = string.IsNullOrWhiteSpace(EditModel.Degree) ? null : EditModel.Degree.Trim(),
                Title = string.IsNullOrWhiteSpace(EditModel.Title) ? null : EditModel.Title.Trim(),
                Phone = string.IsNullOrWhiteSpace(EditModel.Phone) ? null : EditModel.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(EditModel.Email) ? null : EditModel.Email.Trim(),
            DepartmentId = SelectedDepartmentId,
            SectionId = SelectedSectionId
            };

            var createdOk = await ExecuteMutationAsync(() => _dataService.AddAsync(created, cancellationToken)).ConfigureAwait(true);
            if (!createdOk)
            {
                if (string.IsNullOrWhiteSpace(EditorStatusMessage))
                {
                    SetEditorError("Не удалось создать сотрудника. Проверьте корректность выбранных связанных данных и повторите попытку.");
                }
                return false;
            }

            var createdEntity = (await _dataService.GetEmployeesAsync(cancellationToken: cancellationToken).ConfigureAwait(true))
                .Where(x =>
                    string.Equals(x.Surname, surname, System.StringComparison.Ordinal) &&
                    string.Equals(x.Name, name, System.StringComparison.Ordinal) &&
                    string.Equals(x.Patronymic ?? string.Empty, created.Patronymic ?? string.Empty, System.StringComparison.Ordinal))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (createdEntity is null)
            {
                SetEditorError("Сотрудник создан, но не удалось открыть его карточку.");
                return false;
            }

            IsCreateMode = false;
            Id = createdEntity.Id;
            Title = $"{createdEntity.Surname} {createdEntity.Name} {createdEntity.Patronymic}".Trim();
            await LoadAsync().ConfigureAwait(true);
            StatusMessage = "Запись создана.";
            return true;
        }

        if (Employee is null)
        {
            SetEditorError("Нет данных для редактирования.");
            return false;
        }

        var updated = new Employee
        {
            Id = Employee.Id,
            Surname = surname,
            Name = name,
            Patronymic = string.IsNullOrWhiteSpace(EditModel.Patronymic) ? null : EditModel.Patronymic.Trim(),
            Degree = string.IsNullOrWhiteSpace(EditModel.Degree) ? null : EditModel.Degree.Trim(),
            Title = string.IsNullOrWhiteSpace(EditModel.Title) ? null : EditModel.Title.Trim(),
            Phone = string.IsNullOrWhiteSpace(EditModel.Phone) ? null : EditModel.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(EditModel.Email) ? null : EditModel.Email.Trim(),
            DepartmentId = SelectedDepartmentId,
            SectionId = SelectedSectionId
        };

        var ok = await ExecuteMutationAsync(() => _dataService.UpdateAsync(updated, cancellationToken)).ConfigureAwait(true);
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения. Проверьте, что выбранные кафедра и секция существуют и доступны.");
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

        var ok = await _dataService.DeleteAsync<Employee>(Id, cancellationToken).ConfigureAwait(true);
        if (!ok)
        {
            SetEditorError("Удаление недоступно.");
            return false;
        }

        Employee = null;
        StatusMessage = "Запись удалена.";
        return true;
    }

    partial void OnEmployeeChanged(Employee? value)
    {
        NavigateToDepartmentCommand.NotifyCanExecuteChanged();
        NavigateToSectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditModelChanged(EmployeeEditModel value) => TrackEditModelValidity(value);

    [RelayCommand(CanExecute = nameof(CanNavigateDepartment))]
    private void NavigateToDepartment(Department? department) => DetailPageNavigator.ToDepartment(department);

    private static bool CanNavigateDepartment(Department? department) => department is not null;

    [RelayCommand(CanExecute = nameof(CanNavigateSection))]
    private void NavigateToSection(Section? section) => DetailPageNavigator.ToSection(section);

    private static bool CanNavigateSection(Section? section) => section is not null;
}
