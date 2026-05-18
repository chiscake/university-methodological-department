using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Models;

namespace UniversityMethodologicalDepartment.App.Pages.Analytics;

public sealed partial class ChartsPageViewModel : ObservableObject
{
    private readonly IChartDataService _chartDataService;

    public ChartsPageViewModel(IChartDataService chartDataService)
    {
        _chartDataService = chartDataService;
        ChartOptions =
        [
            new("Лабораторные часы по кафедрам")
        ];
        SelectedChartOption = ChartOptions[0];
    }

    public ObservableCollection<LabHoursByDepartmentPoint> Points { get; } = [];

    public IReadOnlyList<ChartOptionItem> ChartOptions { get; }

    [ObservableProperty]
    public partial ChartOptionItem SelectedChartOption { get; set; } = null!;

    [ObservableProperty]
    public partial int SelectedSemester { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (SelectedSemester <= 0)
        {
            StatusMessage = "Семестр должен быть больше нуля.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        Points.Clear();

        try
        {
            var rows = await _chartDataService.GetLabHoursByDepartmentAsync(SelectedSemester);
            foreach (var row in rows)
            {
                Points.Add(row);
            }

            if (Points.Count == 0)
            {
                StatusMessage = "Для выбранного семестра данные отсутствуют.";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChartsPageViewModel] Chart data load failed for semester {SelectedSemester}. Exception: {ex}");
            Points.Clear();
            StatusMessage = "Ошибка загрузки данных диаграммы.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public sealed record ChartOptionItem(string Title);
