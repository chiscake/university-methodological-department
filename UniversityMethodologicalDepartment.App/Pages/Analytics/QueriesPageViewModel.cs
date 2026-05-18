using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevWinUI;
using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Services;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Pages.Analytics;

public sealed partial class QueriesPageViewModel : ObservableObject
{
    private readonly IQueryService _queryService;
    private readonly IDataService _dataService;
    private readonly IQueryResultExportService _queryResultExportService;
    private readonly IFilePickerService _filePickerService;

    public QueriesPageViewModel(
        IQueryService queryService,
        IDataService dataService,
        IQueryResultExportService queryResultExportService,
        IFilePickerService filePickerService)
    {
        _queryService = queryService;
        _dataService = dataService;
        _queryResultExportService = queryResultExportService;
        _filePickerService = filePickerService;

        QueryOptions =
        [
            new(QueryScenario.MultiDepartmentDisciplines, "Дисциплины нескольких кафедр"),
            new(QueryScenario.MultiSemesterDisciplines, "Дисциплины более одного семестра"),
            new(QueryScenario.DepartmentDisciplineCounts, "Кафедры и количество дисциплин"),
            new(QueryScenario.DepartmentDisciplineCountsSorted, "Кафедры, отсортированные по количеству дисциплин"),
            new(QueryScenario.LectureLabDifference, "Разница лабораторных и лекционных часов")
        ];

        SelectedQuery = QueryOptions[0];

        ExportFormatOptions =
        [
            new(QueryExportFormat.Excel, "Excel (.xlsx)"),
            new(QueryExportFormat.Word, "Word (.docx)")
        ];
        SelectedExportFormat = ExportFormatOptions[0];
    }

    public IReadOnlyList<QueryOptionItem> QueryOptions { get; }

    public IReadOnlyList<QueryExportFormatItem> ExportFormatOptions { get; }

    [ObservableProperty]
    public partial QueryExportFormatItem SelectedExportFormat { get; set; } = null!;

    public ObservableCollection<Department> Departments { get; } = [];

    public ObservableCollection<QueryResultRow> Results { get; } = [];

    [ObservableProperty]
    public partial QueryOptionItem SelectedQuery { get; set; } = null!;

    [ObservableProperty]
    public partial int SelectedDepartmentId { get; set; }

    [ObservableProperty]
    public partial int SelectedSemester { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExporting { get; set; }

    public Visibility DepartmentControlsVisibility =>
        SelectedQuery.Scenario == QueryScenario.LectureLabDifference
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility SemesterControlsVisibility =>
        SelectedQuery.Scenario == QueryScenario.LectureLabDifference
            ? Visibility.Visible
            : Visibility.Collapsed;

    public bool IsBusy => IsLoading || IsExporting;

    public async Task InitializeAsync()
    {
        var departments = await _dataService.GetDepartmentsAsync();
        Departments.Clear();
        foreach (var department in departments.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            Departments.Add(department);
        }

        if (Departments.Count > 0)
        {
            SelectedDepartmentId = Departments[0].Id;
        }
    }

    partial void OnSelectedQueryChanged(QueryOptionItem value)
    {
        OnPropertyChanged(nameof(DepartmentControlsVisibility));
        OnPropertyChanged(nameof(SemesterControlsVisibility));
        StatusMessage = string.Empty;
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        ExportResultsCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsExportingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        ExportResultsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExportResults))]
    private async Task ExportResultsAsync()
    {
        if (!CanExportResults())
        {
            return;
        }

        IsExporting = true;
        StatusMessage = string.Empty;
        try
        {
            var scenario = SelectedQuery.Scenario;
            var title = SelectedQuery.Title;
            var rows = Results.ToList();
            var format = SelectedExportFormat.Format;

            var payload = await _queryResultExportService.ExportAsync(scenario, title, rows, format).ConfigureAwait(true);
            var extension = _queryResultExportService.GetDefaultExtension(format);
            var suggestedName = _queryResultExportService.GetDefaultFileName(scenario);

            var filePath = await _filePickerService.PickSaveFileAsync(
                suggestedName,
                new Dictionary<string, IReadOnlyList<string>>
                {
                    { "Excel книга", [".xlsx"] },
                    { "Word документ", [".docx"] }
                },
                extension);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusMessage = "Сохранение отменено.";
                return;
            }

            await File.WriteAllBytesAsync(filePath, payload).ConfigureAwait(true);
            StatusMessage = "Файл сохранён.";
            await ExportSuccessDialogHelper.ShowAsync(App.MainWindow, filePath).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка экспорта.";
            await MessageBox.ShowAsync(
                true,
                App.MainWindow,
                $"Ошибка сохранения: {ex.Message}",
                "Экспорт",
                MessageBoxButtons.OK).ConfigureAwait(true);
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanExportResults() => Results.Count > 0 && !IsLoading && !IsExporting;

    [RelayCommand]
    public async Task ExecuteQueryAsync()
    {
        if (IsLoading)
        {
            return;
        }

        if (SelectedQuery.Scenario == QueryScenario.LectureLabDifference &&
            (SelectedDepartmentId <= 0 || SelectedSemester <= 0))
        {
            StatusMessage = "Укажите кафедру и семестр.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        Results.Clear();

        try
        {
            switch (SelectedQuery.Scenario)
            {
                case QueryScenario.MultiDepartmentDisciplines:
                {
                    var rows = await _queryService.GetMultiDepartmentDisciplinesAsync();
                    foreach (var row in rows)
                    {
                        Results.Add(new QueryResultRow(
                            row.DisciplineName,
                            $"Кафедр: {row.DepartmentCount}",
                            $"Семестров: {row.SemesterCount}; Специальностей: {row.SpecialtyCount}",
                            row.Departments));
                    }

                    break;
                }
                case QueryScenario.MultiSemesterDisciplines:
                {
                    var rows = await _queryService.GetMultiSemesterDisciplinesAsync();
                    foreach (var row in rows)
                    {
                        Results.Add(new QueryResultRow(
                            row.DisciplineName,
                            $"Семестров: {row.SemesterCount}",
                            $"Семестры: {row.Semesters}",
                            row.Departments));
                    }

                    break;
                }
                case QueryScenario.DepartmentDisciplineCounts:
                {
                    var rows = await _queryService.GetDepartmentDisciplineCountsAsync(false);
                    foreach (var row in rows)
                    {
                        Results.Add(new QueryResultRow(
                            row.DepartmentName,
                            $"Дисциплин: {row.DisciplineCount}",
                            string.Empty,
                            string.Empty));
                    }

                    break;
                }
                case QueryScenario.DepartmentDisciplineCountsSorted:
                {
                    var rows = await _queryService.GetDepartmentDisciplineCountsAsync(true);
                    foreach (var row in rows)
                    {
                        Results.Add(new QueryResultRow(
                            row.DepartmentName,
                            $"Дисциплин: {row.DisciplineCount}",
                            string.Empty,
                            string.Empty));
                    }

                    break;
                }
                case QueryScenario.LectureLabDifference:
                {
                    var rows = await _queryService.GetLectureLabDifferencesAsync(SelectedDepartmentId, SelectedSemester);
                    foreach (var row in rows)
                    {
                        Results.Add(new QueryResultRow(
                            row.DisciplineName,
                            $"Лекции: {row.LectureHours}; Лабораторные: {row.LabHours}",
                            $"Разница: {row.DifferenceHours}",
                            string.Empty));
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Results.Count == 0)
            {
                StatusMessage = "По выбранным параметрам данные не найдены.";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[QueriesPage] ExecuteQuery failed. Scenario={SelectedQuery.Scenario}, DepartmentId={SelectedDepartmentId}, Semester={SelectedSemester}. Exception: {ex}");
            Results.Clear();
            StatusMessage = "Ошибка выполнения запроса.";
        }
        finally
        {
            IsLoading = false;
            ExportResultsCommand.NotifyCanExecuteChanged();
        }
    }
}
