using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevWinUI;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Services;

namespace UniversityMethodologicalDepartment.App.Pages.Analytics;

public sealed partial class ReportsPageViewModel : ObservableObject
{
    private readonly IReportExportService _reportExportService;
    private readonly IFilePickerService _filePickerService;

    private List<MatrixPreviewRow> _allMatrixRows = [];

    public ReportsPageViewModel(IReportExportService reportExportService, IFilePickerService filePickerService)
    {
        _reportExportService = reportExportService;
        _filePickerService = filePickerService;
        ReportTemplates =
        [
            new(ReportExportKind.DepartmentsSummaryExcel, "Информация о кафедрах (Excel)"),
            new(ReportExportKind.SpecialtyMatrixExcel, "Таблица по каждой специальности (Excel)"),
            new(ReportExportKind.DepartmentDisciplinesWord, "Отчёт по кафедрам о читаемых дисциплинах (Word)")
        ];
        SelectedTemplate = ReportTemplates[0];
    }

    public IReadOnlyList<ReportTemplateItem> ReportTemplates { get; }

    public ObservableCollection<DepartmentSummaryPreviewRow> DepartmentPreviewRows { get; } = [];

    public ObservableCollection<SpecialtyOptionItem> SpecialtyOptions { get; } = [];

    public ObservableCollection<MatrixPreviewRow> MatrixPreviewRows { get; } = [];

    public ObservableCollection<WordDepartmentPreviewSection> WordSections { get; } = [];

    [ObservableProperty]
    public partial ReportTemplateItem SelectedTemplate { get; set; } = null!;

    [ObservableProperty]
    public partial int SelectedSpecialtyId { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasPreview { get; set; }

    public bool PreviewDepartmentsVisible =>
        HasPreview && SelectedTemplate.Kind == ReportExportKind.DepartmentsSummaryExcel;

    public bool PreviewMatrixVisible =>
        HasPreview && SelectedTemplate.Kind == ReportExportKind.SpecialtyMatrixExcel;

    public bool PreviewWordVisible =>
        HasPreview && SelectedTemplate.Kind == ReportExportKind.DepartmentDisciplinesWord;

    partial void OnSelectedTemplateChanged(ReportTemplateItem value)
    {
        ClearPreviewState();
        OnPropertyChanged(nameof(PreviewDepartmentsVisible));
        OnPropertyChanged(nameof(PreviewMatrixVisible));
        OnPropertyChanged(nameof(PreviewWordVisible));
    }

    partial void OnHasPreviewChanged(bool value)
    {
        ExportToFileCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PreviewDepartmentsVisible));
        OnPropertyChanged(nameof(PreviewMatrixVisible));
        OnPropertyChanged(nameof(PreviewWordVisible));
    }

    partial void OnIsBusyChanged(bool value)
    {
        ExportToFileCommand.NotifyCanExecuteChanged();
        BuildPreviewCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSpecialtyIdChanged(int value)
    {
        RefreshMatrixFilter();
    }

    [RelayCommand(CanExecute = nameof(CanBuildPreview))]
    private async Task BuildPreviewAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        ClearPreviewCollections();

        try
        {
            var doc = await _reportExportService.GetPreviewAsync(SelectedTemplate.Kind);
            ApplyPreview(doc);
            HasPreview = true;
            StatusMessage = "Предпросмотр готов. При необходимости сохраните отчёт в файл.";
        }
        catch (Exception ex)
        {
            HasPreview = false;
            StatusMessage = "Ошибка построения предпросмотра.";
            await MessageBox.ShowAsync(
                true,
                App.MainWindow,
                $"Ошибка: {ex.Message}",
                "Отчёты",
                MessageBoxButtons.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanBuildPreview() => !IsBusy;

    private void ApplyPreview(ReportPreviewDocument doc)
    {
        switch (doc.Kind)
        {
            case ReportExportKind.DepartmentsSummaryExcel:
                foreach (var row in doc.DepartmentRows ?? [])
                {
                    DepartmentPreviewRows.Add(row);
                }

                break;
            case ReportExportKind.SpecialtyMatrixExcel:
                foreach (var opt in doc.SpecialtyOptions ?? [])
                {
                    SpecialtyOptions.Add(opt);
                }

                _allMatrixRows = doc.MatrixRows?.ToList() ?? [];
                if (SpecialtyOptions.Count > 0)
                {
                    SelectedSpecialtyId = SpecialtyOptions[0].Id;
                }

                RefreshMatrixFilter();

                break;
            case ReportExportKind.DepartmentDisciplinesWord:
                foreach (var section in doc.WordSections ?? [])
                {
                    WordSections.Add(section);
                }

                break;
        }
    }

    private void RefreshMatrixFilter()
    {
        MatrixPreviewRows.Clear();
        if (_allMatrixRows.Count == 0)
        {
            return;
        }

        var specialtyId = SelectedSpecialtyId;
        if (specialtyId == 0 && SpecialtyOptions.Count > 0)
        {
            specialtyId = SpecialtyOptions[0].Id;
        }

        foreach (var row in _allMatrixRows.Where(r => r.SpecialtyId == specialtyId))
        {
            MatrixPreviewRows.Add(row);
        }
    }

    private void ClearPreviewState()
    {
        ClearPreviewCollections();
        HasPreview = false;
    }

    private void ClearPreviewCollections()
    {
        DepartmentPreviewRows.Clear();
        SpecialtyOptions.Clear();
        MatrixPreviewRows.Clear();
        _allMatrixRows.Clear();
        WordSections.Clear();
        SelectedSpecialtyId = 0;
        OnPropertyChanged(nameof(PreviewDepartmentsVisible));
        OnPropertyChanged(nameof(PreviewMatrixVisible));
        OnPropertyChanged(nameof(PreviewWordVisible));
    }

    [RelayCommand(CanExecute = nameof(CanExportToFile))]
    private async Task ExportToFileAsync()
    {
        if (!HasPreview || IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var kind = SelectedTemplate.Kind;
            int? selectedSpecialtyId = kind == ReportExportKind.SpecialtyMatrixExcel && SelectedSpecialtyId > 0
                ? SelectedSpecialtyId
                : null;
            var payload = await _reportExportService.GenerateAsync(kind, selectedSpecialtyId);
            var extension = _reportExportService.GetDefaultExtension(kind);

            var filePath = await _filePickerService.PickSaveFileAsync(
                _reportExportService.GetDefaultFileName(kind),
                new Dictionary<string, IReadOnlyList<string>>
                {
                    { extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) ? "Word документ" : "Excel документ", [extension] }
                },
                extension);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusMessage = "Сохранение отменено.";
                return;
            }

            await File.WriteAllBytesAsync(filePath, payload);
            StatusMessage = "Файл сохранён.";
            await ExportSuccessDialogHelper.ShowAsync(App.MainWindow, filePath);
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка экспорта.";
            await MessageBox.ShowAsync(
                true,
                App.MainWindow,
                $"Ошибка сохранения: {ex.Message}",
                "Экспорт",
                MessageBoxButtons.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExportToFile() => HasPreview && !IsBusy;
}
