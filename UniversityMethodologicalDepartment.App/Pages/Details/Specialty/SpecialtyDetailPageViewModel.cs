using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Helpers;
using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.ViewModels.Details.Validation;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.ViewModels.Details;

/// <summary>
/// ViewModel страницы деталей специальности.
/// </summary>
public partial class SpecialtyDetailPageViewModel : DetailEditorViewModelBase
{
    private readonly IDataService _dataService;

    public SpecialtyDetailPageViewModel(IDataService dataService, IAuthService authService)
        : base(authService)
    {
        _dataService = dataService;
        TrackEditModelValidity(EditModel);
    }

    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCreateMode { get; set; }

    [ObservableProperty]
    public partial Specialty? Specialty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SpecialtyEditModel EditModel { get; set; } = new();

    public ObservableCollection<CurriculumItem> CurriculumItems { get; } = [];

    public sealed partial class SpecialtyEditModel : ValidatedEditModelBase
    {
        public SpecialtyEditModel()
        {
            ValidateCode(Code);
            ValidateName(Name);
            ValidateQualification(Qualification);
            ValidateDuration(Duration);
            ValidateFormOfStudy(FormOfStudy);
        }

        [ObservableProperty]
        public partial string Code { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Qualification { get; set; }

        [ObservableProperty]
        public partial string? Duration { get; set; }

        [ObservableProperty]
        public partial string? FormOfStudy { get; set; }

        partial void OnCodeChanged(string value) => ValidateCode(value);

        partial void OnNameChanged(string value) => ValidateName(value);

        partial void OnQualificationChanged(string? value) => ValidateQualification(value);

        partial void OnDurationChanged(string? value) => ValidateDuration(value);

        partial void OnFormOfStudyChanged(string? value) => ValidateFormOfStudy(value);

        private void ValidateCode(string value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                errors.Add("Код специальности обязателен.");
            }
            else if (trimmed.Length > 50)
            {
                errors.Add("Код специальности не должен превышать 50 символов.");
            }

            SetErrors(nameof(Code), errors);
        }

        private void ValidateName(string value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                errors.Add("Название специальности обязательно.");
            }
            else if (trimmed.Length > 200)
            {
                errors.Add("Название специальности не должно превышать 200 символов.");
            }

            SetErrors(nameof(Name), errors);
        }

        private void ValidateQualification(string? value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim();
            if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > 100)
            {
                errors.Add("Квалификация не должна превышать 100 символов.");
            }

            SetErrors(nameof(Qualification), errors);
        }

        private void ValidateDuration(string? value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                if (!int.TryParse(trimmed, out var duration))
                {
                    errors.Add("Продолжительность должна быть целым числом.");
                }
                else if (duration <= 0)
                {
                    errors.Add("Продолжительность должна быть больше 0.");
                }
            }

            SetErrors(nameof(Duration), errors);
        }

        private void ValidateFormOfStudy(string? value)
        {
            var errors = new List<string>();
            var trimmed = value?.Trim();
            if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > 100)
            {
                errors.Add("Форма обучения не должна превышать 100 символов.");
            }

            SetErrors(nameof(FormOfStudy), errors);
        }
    }

    public void Initialize(EntityDetailParameter parameter)
    {
        Id = parameter.Id;
        Title = string.IsNullOrWhiteSpace(parameter.Name) ? "Специальность" : parameter.Name;
        IsCreateMode = parameter.IsCreateMode;
        if (IsCreateMode)
        {
            Specialty = null;
            CurriculumItems.Clear();
            StatusMessage = string.Empty;
            PrepareEditModel();
        }
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        Specialty = null;
        CurriculumItems.Clear();
        try
        {
            var entity = await _dataService.GetByIdAsync<Specialty>(Id).ConfigureAwait(true);
            Specialty = entity;
            if (entity is not null)
            {
                var curriculumItems = await _dataService.GetCurriculumItemsAsync(specialtyId: entity.Id).ConfigureAwait(true);
                foreach (var item in curriculumItems.OrderBy(x => x.Semester).ThenBy(x => x.Discipline?.Name))
                {
                    CurriculumItems.Add(item);
                }
            }

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
        if (Specialty is null)
        {
            EditModel = new SpecialtyEditModel();
            return;
        }

        EditModel = new SpecialtyEditModel
        {
            Code = Specialty.Code,
            Name = Specialty.Name,
            Qualification = Specialty.Qualification,
            Duration = Specialty.Duration?.ToString(),
            FormOfStudy = Specialty.FormOfStudy
        };
    }

    protected override async Task<bool> SaveEditorCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var code = EditModel.Code.Trim();
        var name = EditModel.Name.Trim();
        var qualification = string.IsNullOrWhiteSpace(EditModel.Qualification) ? null : EditModel.Qualification.Trim();
        var formOfStudy = string.IsNullOrWhiteSpace(EditModel.FormOfStudy) ? null : EditModel.FormOfStudy.Trim();
        var duration = ParseDuration(EditModel.Duration);

        if (string.IsNullOrWhiteSpace(code))
        {
            SetEditorError("Код специальности не может быть пустым.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            SetEditorError("Название специальности не может быть пустым.");
            return false;
        }

        if (IsCreateMode)
        {
            var created = new Specialty
            {
                Code = code,
                Name = name,
                Qualification = qualification,
                Duration = duration,
                FormOfStudy = formOfStudy
            };

            var createdOk = await ExecuteMutationAsync(() => _dataService.AddAsync(created, cancellationToken)).ConfigureAwait(true);
            if (!createdOk)
            {
                if (string.IsNullOrWhiteSpace(EditorStatusMessage))
                {
                    SetEditorError("Не удалось создать специальность. Проверьте корректность данных и повторите попытку.");
                }
                return false;
            }

            var createdEntity = (await _dataService.GetSpecialtiesAsync(cancellationToken).ConfigureAwait(true))
                .Where(x => string.Equals(x.Code, code, System.StringComparison.OrdinalIgnoreCase)
                            && string.Equals(x.Name, name, System.StringComparison.Ordinal))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (createdEntity is null)
            {
                SetEditorError("Специальность создана, но не удалось открыть её карточку.");
                return false;
            }

            IsCreateMode = false;
            Id = createdEntity.Id;
            Title = createdEntity.Name;
            await LoadAsync().ConfigureAwait(true);
            StatusMessage = "Запись создана.";
            return true;
        }

        if (Specialty is null)
        {
            SetEditorError("Нет данных для редактирования.");
            return false;
        }

        var updated = new Specialty
        {
            Id = Specialty.Id,
            Code = code,
            Name = name,
            Qualification = qualification,
            Duration = duration,
            FormOfStudy = formOfStudy
        };

        var ok = await ExecuteMutationAsync(() => _dataService.UpdateAsync(updated, cancellationToken)).ConfigureAwait(true);
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(EditorStatusMessage))
            {
                SetEditorError("Не удалось сохранить изменения. Проверьте корректность данных и повторите попытку.");
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

        var ok = await _dataService.DeleteAsync<Specialty>(Id, cancellationToken).ConfigureAwait(true);
        if (!ok)
        {
            SetEditorError("Удаление недоступно.");
            return false;
        }

        Specialty = null;
        CurriculumItems.Clear();
        StatusMessage = "Запись удалена.";
        return true;
    }

    partial void OnSpecialtyChanged(Specialty? value)
    {
        NavigateToCurriculumItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditModelChanged(SpecialtyEditModel value) => TrackEditModelValidity(value);

    [RelayCommand(CanExecute = nameof(CanNavigateCurriculumItem))]
    private void NavigateToCurriculumItem(CurriculumItem? item) => DetailPageNavigator.ToCurriculumItem(item);

    private static bool CanNavigateCurriculumItem(CurriculumItem? item) => item is not null;

    private static int? ParseDuration(string? durationText)
    {
        if (string.IsNullOrWhiteSpace(durationText))
        {
            return null;
        }

        return int.TryParse(durationText.Trim(), out var duration) ? duration : null;
    }
}
