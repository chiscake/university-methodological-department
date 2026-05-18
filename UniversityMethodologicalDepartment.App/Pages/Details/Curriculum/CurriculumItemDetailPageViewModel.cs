using System;
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
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Bus.Services.Errors;

namespace UniversityMethodologicalDepartment.App.ViewModels.Details;

/// <summary>
/// ViewModel страницы деталей элемента учебного плана.
/// </summary>
public partial class CurriculumItemDetailPageViewModel : DetailEditorViewModelBase
{
    private readonly IDataService _dataService;
    private readonly IReferenceSearchService _referenceSearchService;
    private bool _isSyncingDisciplineQuery;
    private bool _isSyncingSpecialtyQuery;
    private string _selectedDisciplineTitle = string.Empty;
    private string _selectedSpecialtyTitle = string.Empty;

    public CurriculumItemDetailPageViewModel(
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
    public partial CurriculumItem? CurriculumItem { get; set; }

    public int TotalHours =>
        (CurriculumItem?.LectureHours ?? 0)
        + (CurriculumItem?.LabHours ?? 0)
        + (CurriculumItem?.UsrHours ?? 0)
        + (CurriculumItem?.PracticalHours ?? 0);

    public string ControlFormDisplay => CurriculumItem switch
    {
        null => string.Empty,
        { IsExam: true } => "Экзамен",
        { IsCredit: true } => "Зачет",
        _ => "Не задано"
    };

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CurriculumItemEditModel EditModel { get; set; } = new();

    [ObservableProperty]
    public partial int? SelectedDisciplineId { get; set; }

    [ObservableProperty]
    public partial string DisciplineQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? SelectedSpecialtyId { get; set; }

    [ObservableProperty]
    public partial string SpecialtyQuery { get; set; } = string.Empty;

    public ObservableCollection<ReferenceSearchItem> DisciplineSuggestions { get; } = [];

    public ObservableCollection<ReferenceSearchItem> SpecialtySuggestions { get; } = [];

    public sealed partial class CurriculumItemEditModel : ValidatedEditModelBase
    {
        private const int MinSemester = 1;
        private const int MaxSemester = 12;
        private const int MinHours = 0;
        private const int MaxHours = 1000;

        public CurriculumItemEditModel()
        {
            IsCredit = true;
            IsExam = false;
            SelectedControlFormIndex = 0;
            ValidateSemester(Semester);
            ValidateHours(nameof(LectureHours), LectureHours, "Лекционные часы");
            ValidateHours(nameof(LabHours), LabHours, "Лабораторные часы");
            ValidateHours(nameof(UsrHours), UsrHours, "Часы УСР");
            ValidateHours(nameof(PracticalHours), PracticalHours, "Практические часы");
        }

        [ObservableProperty]
        public partial int Semester { get; set; }

        [ObservableProperty]
        public partial int LectureHours { get; set; }

        [ObservableProperty]
        public partial int LabHours { get; set; }

        [ObservableProperty]
        public partial int UsrHours { get; set; }

        [ObservableProperty]
        public partial int PracticalHours { get; set; }

        [ObservableProperty]
        public partial bool HasCourseProject { get; set; }

        [ObservableProperty]
        public partial bool IsCredit { get; set; }

        [ObservableProperty]
        public partial bool IsExam { get; set; }

        [ObservableProperty]
        public partial int SelectedControlFormIndex { get; set; }

        public bool IsCreditControlAvailable => !HasCourseProject;

        partial void OnSemesterChanged(int value) => ValidateSemester(value);

        partial void OnLectureHoursChanged(int value) =>
            ValidateHours(nameof(LectureHours), value, "Лекционные часы");

        partial void OnLabHoursChanged(int value) =>
            ValidateHours(nameof(LabHours), value, "Лабораторные часы");

        partial void OnUsrHoursChanged(int value) =>
            ValidateHours(nameof(UsrHours), value, "Часы УСР");

        partial void OnPracticalHoursChanged(int value) =>
            ValidateHours(nameof(PracticalHours), value, "Практические часы");

        partial void OnHasCourseProjectChanged(bool value)
        {
            if (value)
            {
                IsExam = true;
                IsCredit = false;
                if (SelectedControlFormIndex != 1)
                {
                    SelectedControlFormIndex = 1;
                }
            }

            OnPropertyChanged(nameof(IsCreditControlAvailable));
        }

        partial void OnSelectedControlFormIndexChanged(int value)
        {
            if (value == 0 && !IsCreditControlAvailable)
            {
                if (SelectedControlFormIndex != 1)
                {
                    SelectedControlFormIndex = 1;
                }

                IsCredit = false;
                IsExam = true;
                return;
            }

            IsCredit = value == 0;
            IsExam = value == 1;
        }

        partial void OnIsExamChanged(bool value)
        {
            if (value)
            {
                IsCredit = false;
                if (SelectedControlFormIndex != 1)
                {
                    SelectedControlFormIndex = 1;
                }
            }
        }

        partial void OnIsCreditChanged(bool value)
        {
            if (value)
            {
                if (!IsCreditControlAvailable)
                {
                    IsCredit = false;
                    if (!IsExam)
                    {
                        IsExam = true;
                    }

                    if (SelectedControlFormIndex != 1)
                    {
                        SelectedControlFormIndex = 1;
                    }

                    return;
                }

                IsExam = false;
                if (SelectedControlFormIndex != 0)
                {
                    SelectedControlFormIndex = 0;
                }
            }
        }

        private void ValidateSemester(int value)
        {
            var errors = new List<string>();

            if (value < MinSemester || value > MaxSemester)
            {
                errors.Add($"Семестр должен быть от {MinSemester} до {MaxSemester}.");
            }

            SetErrors(nameof(Semester), errors);
        }

        private void ValidateHours(string propertyName, int value, string fieldLabel)
        {
            var errors = new List<string>();

            if (value < MinHours || value > MaxHours)
            {
                errors.Add($"{fieldLabel}: значение должно быть от {MinHours} до {MaxHours}.");
            }

            SetErrors(propertyName, errors);
        }
    }

    /// <summary>Инициализирует данные из параметра навигации.</summary>
    public void Initialize(EntityDetailParameter parameter)
    {
        Id = parameter.Id;
        Title = string.IsNullOrWhiteSpace(parameter.Name) ? "Элемент учебного плана" : parameter.Name;
        IsCreateMode = parameter.IsCreateMode;
        if (IsCreateMode)
        {
            CurriculumItem = null;
            StatusMessage = string.Empty;
            PrepareEditModel();
        }
    }

    /// <summary>Загружает элемент учебного плана с дисциплиной и специальностью из БД.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        CurriculumItem = null;
        try
        {
            var entity = await _dataService.GetCurriculumItemWithDetailsAsync(Id).ConfigureAwait(true);
            CurriculumItem = entity;
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
        if (CurriculumItem is null)
        {
            EditModel = new CurriculumItemEditModel();
            SetDisciplineSelection(null, string.Empty);
            SetSpecialtySelection(null, string.Empty);
            DisciplineSuggestions.Clear();
            SpecialtySuggestions.Clear();
            return;
        }

        EditModel = new CurriculumItemEditModel
        {
            Semester = CurriculumItem.Semester,
            LectureHours = CurriculumItem.LectureHours,
            LabHours = CurriculumItem.LabHours,
            UsrHours = CurriculumItem.UsrHours,
            PracticalHours = CurriculumItem.PracticalHours,
            HasCourseProject = CurriculumItem.HasCourseProject,
            IsCredit = CurriculumItem.IsCredit,
            IsExam = CurriculumItem.IsExam
        };
        SetDisciplineSelection(CurriculumItem.DisciplineId, CurriculumItem.Discipline?.Name ?? string.Empty);
        SetSpecialtySelection(CurriculumItem.SpecialtyId, CurriculumItem.Specialty?.Name ?? string.Empty);
        DisciplineSuggestions.Clear();
        SpecialtySuggestions.Clear();
    }

    public async Task UpdateDisciplineSuggestionsAsync(string? query)
    {
        var results = await _referenceSearchService
            .SearchAsync(query ?? string.Empty, 20, ReferenceSearchScope.Discipline)
            .ConfigureAwait(true);

        DisciplineSuggestions.Clear();
        foreach (var item in results)
        {
            DisciplineSuggestions.Add(item);
        }
    }

    public void SelectDisciplineSuggestion(ReferenceSearchItem suggestion)
    {
        SetDisciplineSelection(suggestion.Id, suggestion.Title);
    }

    public void SelectDisciplineFirstSuggestion()
    {
        if (DisciplineSuggestions.Count > 0)
        {
            SelectDisciplineSuggestion(DisciplineSuggestions[0]);
        }
    }

    public async Task UpdateSpecialtySuggestionsAsync(string? query)
    {
        var results = await _referenceSearchService
            .SearchAsync(query ?? string.Empty, 20, ReferenceSearchScope.Specialty)
            .ConfigureAwait(true);

        SpecialtySuggestions.Clear();
        foreach (var item in results)
        {
            SpecialtySuggestions.Add(item);
        }
    }

    public void SelectSpecialtySuggestion(ReferenceSearchItem suggestion)
    {
        SetSpecialtySelection(suggestion.Id, suggestion.Title);
    }

    public void SelectSpecialtyFirstSuggestion()
    {
        if (SpecialtySuggestions.Count > 0)
        {
            SelectSpecialtySuggestion(SpecialtySuggestions[0]);
        }
    }

    protected override async Task<bool> SaveEditorCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureDisciplineSelectionAsync(cancellationToken).ConfigureAwait(true);
        await EnsureSpecialtySelectionAsync(cancellationToken).ConfigureAwait(true);

        if (EditModel.Semester <= 0)
        {
            SetEditorError("Семестр должен быть больше 0.");
            return false;
        }

        if (SelectedDisciplineId is null or <= 0)
        {
            SetEditorError(string.IsNullOrWhiteSpace(DisciplineQuery)
                ? "Выберите дисциплину."
                : "Значение в поле \"Дисциплина\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (SelectedSpecialtyId is null or <= 0)
        {
            SetEditorError(string.IsNullOrWhiteSpace(SpecialtyQuery)
                ? "Выберите специальность."
                : "Значение в поле \"Специальность\" не найдено. Выберите вариант из списка подсказок.");
            return false;
        }

        if (IsCreateMode)
        {
            var discipline = await _dataService.GetByIdAsync<Discipline>(SelectedDisciplineId.Value, cancellationToken).ConfigureAwait(true);
            if (discipline is null)
            {
                SetEditorError("Невозможно создать элемент плана: выбранная дисциплина не найдена.");
                return false;
            }

            var specialty = await _dataService.GetByIdAsync<Specialty>(SelectedSpecialtyId.Value, cancellationToken).ConfigureAwait(true);
            if (specialty is null)
            {
                SetEditorError("Невозможно создать элемент плана: выбранная специальность не найдена.");
                return false;
            }

            var created = new CurriculumItem
            {
                DisciplineId = discipline.Id,
                SpecialtyId = specialty.Id,
                Semester = EditModel.Semester,
                LectureHours = EditModel.LectureHours,
                LabHours = EditModel.LabHours,
                UsrHours = EditModel.UsrHours,
                PracticalHours = EditModel.PracticalHours,
                HasCourseProject = EditModel.HasCourseProject,
                IsCredit = EditModel.IsCredit,
                IsExam = EditModel.IsExam
            };

            var createdOk = await ExecuteMutationAsync(() => _dataService.AddAsync(created, cancellationToken)).ConfigureAwait(true);

            if (!createdOk)
            {
                if (string.IsNullOrWhiteSpace(EditorStatusMessage))
                {
                    SetEditorError("Не удалось создать элемент плана. Проверьте корректность выбранных дисциплины и специальности.");
                }
                return false;
            }

            var createdEntity = (await _dataService.GetCurriculumItemsAsync(specialty.Id, discipline.Id, cancellationToken).ConfigureAwait(true))
                .Where(x =>
                    x.Semester == created.Semester &&
                    x.LectureHours == created.LectureHours &&
                    x.LabHours == created.LabHours &&
                    x.UsrHours == created.UsrHours &&
                    x.PracticalHours == created.PracticalHours &&
                    x.HasCourseProject == created.HasCourseProject &&
                    x.IsCredit == created.IsCredit &&
                    x.IsExam == created.IsExam)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (createdEntity is null)
            {
                SetEditorError("Элемент плана создан, но не удалось открыть его карточку.");
                return false;
            }

            IsCreateMode = false;
            Id = createdEntity.Id;
            Title = $"{createdEntity.Discipline.Name} / {createdEntity.Specialty.Name}";
            await LoadAsync().ConfigureAwait(true);
            StatusMessage = "Запись создана.";
            return true;
        }

        if (CurriculumItem is null)
        {
            SetEditorError("Нет данных для редактирования.");
            return false;
        }

        var updated = new CurriculumItem
        {
            Id = CurriculumItem.Id,
            DisciplineId = SelectedDisciplineId.Value,
            SpecialtyId = SelectedSpecialtyId.Value,
            Semester = EditModel.Semester,
            LectureHours = EditModel.LectureHours,
            LabHours = EditModel.LabHours,
            UsrHours = EditModel.UsrHours,
            PracticalHours = EditModel.PracticalHours,
            HasCourseProject = EditModel.HasCourseProject,
            IsCredit = EditModel.IsCredit,
            IsExam = EditModel.IsExam
        };

        var ok = await ExecuteMutationAsync(() => _dataService.UpdateAsync(updated, cancellationToken)).ConfigureAwait(true);
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения. Проверьте, что выбранные дисциплина и специальность существуют и доступны.");
            }
            return false;
        }

        await LoadAsync().ConfigureAwait(true);
        return true;
    }

    public void HandleDisciplineQueryUserInput(string queryText)
    {
        if (_isSyncingDisciplineQuery)
        {
            return;
        }

        if (SelectedDisciplineId.HasValue &&
            !string.Equals(queryText, _selectedDisciplineTitle, StringComparison.Ordinal))
        {
            SelectedDisciplineId = null;
        }
    }

    public void HandleSpecialtyQueryUserInput(string queryText)
    {
        if (_isSyncingSpecialtyQuery)
        {
            return;
        }

        if (SelectedSpecialtyId.HasValue &&
            !string.Equals(queryText, _selectedSpecialtyTitle, StringComparison.Ordinal))
        {
            SelectedSpecialtyId = null;
        }
    }

    private void SetDisciplineSelection(int? disciplineId, string? title)
    {
        _isSyncingDisciplineQuery = true;
        try
        {
            SelectedDisciplineId = disciplineId;
            DisciplineQuery = title ?? string.Empty;
            _selectedDisciplineTitle = DisciplineQuery;
        }
        finally
        {
            _isSyncingDisciplineQuery = false;
        }
    }

    private void SetSpecialtySelection(int? specialtyId, string? title)
    {
        _isSyncingSpecialtyQuery = true;
        try
        {
            SelectedSpecialtyId = specialtyId;
            SpecialtyQuery = title ?? string.Empty;
            _selectedSpecialtyTitle = SpecialtyQuery;
        }
        finally
        {
            _isSyncingSpecialtyQuery = false;
        }
    }

    private async Task EnsureDisciplineSelectionAsync(CancellationToken cancellationToken)
    {
        if (SelectedDisciplineId is > 0 || string.IsNullOrWhiteSpace(DisciplineQuery))
        {
            return;
        }

        var exactMatch = await FindExactReferenceAsync(
            DisciplineQuery,
            ReferenceSearchScope.Discipline,
            cancellationToken).ConfigureAwait(true);

        if (exactMatch is not null)
        {
            SetDisciplineSelection(exactMatch.Id, exactMatch.Title);
        }
    }

    private async Task EnsureSpecialtySelectionAsync(CancellationToken cancellationToken)
    {
        if (SelectedSpecialtyId is > 0 || string.IsNullOrWhiteSpace(SpecialtyQuery))
        {
            return;
        }

        var exactMatch = await FindExactReferenceAsync(
            SpecialtyQuery,
            ReferenceSearchScope.Specialty,
            cancellationToken).ConfigureAwait(true);

        if (exactMatch is not null)
        {
            SetSpecialtySelection(exactMatch.Id, exactMatch.Title);
        }
    }

    private async Task<ReferenceSearchItem?> FindExactReferenceAsync(
        string query,
        ReferenceSearchScope scope,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var trimmedQuery = query.Trim();
        var results = await _referenceSearchService
            .SearchAsync(trimmedQuery, 20, scope)
            .ConfigureAwait(true);

        return results.FirstOrDefault(item =>
            string.Equals(item.Title, trimmedQuery, StringComparison.OrdinalIgnoreCase));
    }

    protected override async Task<bool> DeleteEntityCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsCreateMode)
        {
            SetEditorError("Новая запись ещё не создана.");
            return false;
        }

        var ok = await _dataService.DeleteAsync<CurriculumItem>(Id, cancellationToken).ConfigureAwait(true);
        if (!ok)
        {
            SetEditorError("Удаление недоступно.");
            return false;
        }

        CurriculumItem = null;
        StatusMessage = "Запись удалена.";
        return true;
    }

    partial void OnCurriculumItemChanged(CurriculumItem? value)
    {
        NavigateToDisciplineCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(TotalHours));
        OnPropertyChanged(nameof(ControlFormDisplay));
    }

    partial void OnEditModelChanged(CurriculumItemEditModel value) => TrackEditModelValidity(value);

    [RelayCommand(CanExecute = nameof(CanNavigateDiscipline))]
    private void NavigateToDiscipline(Discipline? discipline) => DetailPageNavigator.ToDiscipline(discipline);

    private static bool CanNavigateDiscipline(Discipline? discipline) => discipline is not null;
}
