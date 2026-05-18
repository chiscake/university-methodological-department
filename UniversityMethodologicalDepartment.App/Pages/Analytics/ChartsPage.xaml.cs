using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using UniversityMethodologicalDepartment.App.Controls;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Services;

namespace UniversityMethodologicalDepartment.App.Pages.Analytics;

public sealed partial class ChartsPage : Page
{
    public ChartsPageViewModel ViewModel { get; }

    public ChartsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ChartsPageViewModel>();
        ViewModel.Points.CollectionChanged += (_, _) => RenderChart();
        DataContext = ViewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Points.Count == 0 && !ViewModel.IsLoading)
        {
            await ViewModel.LoadAsync();
        }

        RenderChart();
    }

    private void RenderChart()
    {
        var values = ViewModel.Points.Select(x => x.TotalLabHours).ToArray();
        var labels = ViewModel.Points.Select(x => WrapLabel(x.DepartmentName)).ToArray();
        var labelsRotation = labels.Length switch
        {
            <= 6 => 0,
            <= 12 => 25,
            _ => 45
        };
        var labelsTextSize = labels.Length > 12 ? 10 : 12;

        DepartmentHoursChart.Series =
        [
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Лабораторные часы",
                MaxBarWidth = 72
            }
        ];

        DepartmentHoursChart.XAxes =
        [
            new Axis
            {
                Labels = labels.ToList(),
                LabelsRotation = labelsRotation,
                TextSize = labelsTextSize
            }
        ];

        DepartmentHoursChart.YAxes =
        [
            new Axis
            {
                Name = "Часы",
                MinLimit = 0,
                MinStep = 1
            }
        ];

        DepartmentHoursChart.TooltipBackgroundPaint = new SolidColorPaint(new SKColor(28, 28, 28, 220));
        DepartmentHoursChart.TooltipTextPaint = new SolidColorPaint(SKColors.White);
        UpdateChartWidth(labels.Length);
    }

    private void ChartScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateChartWidth(ViewModel.Points.Count);

    private void UpdateChartWidth(int pointCount)
    {
        var viewportWidth = ChartScrollViewer.ActualWidth;
        if (viewportWidth <= 0)
        {
            return;
        }

        var chartMinimumWidth = Math.Max(viewportWidth - 32, pointCount * 120d);
        DepartmentHoursChart.MinWidth = Math.Max(chartMinimumWidth, 640);
    }

    private static string WrapLabel(string text, int maxLineLength = 18)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLineLength)
        {
            return text;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
        {
            return text;
        }

        var builder = new StringBuilder();
        var currentLineLength = 0;

        foreach (var word in words)
        {
            var additionalLength = currentLineLength == 0 ? word.Length : word.Length + 1;
            if (currentLineLength > 0 && currentLineLength + additionalLength > maxLineLength)
            {
                builder.AppendLine();
                builder.Append(word);
                currentLineLength = word.Length;
            }
            else
            {
                if (currentLineLength > 0)
                {
                    builder.Append(' ');
                    currentLineLength++;
                }

                builder.Append(word);
                currentLineLength += word.Length;
            }
        }

        return builder.ToString();
    }

    private async void SemesterSelector_SemesterChanged(object? sender, int _) =>
        await ViewModel.LoadAsync();

    private async void ExportChartButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsLoading)
        {
            ViewModel.StatusMessage = "Дождитесь завершения загрузки данных.";
            return;
        }

        if (ViewModel.Points.Count == 0)
        {
            ViewModel.StatusMessage = "Нет данных для экспорта диаграммы.";
            return;
        }

        try
        {
            var picker = App.Services.GetRequiredService<IFilePickerService>();
            var filePath = await picker.PickSaveFileAsync(
                $"lab_hours_semester_{ViewModel.SelectedSemester}_{DateTime.Now:yyyyMMdd_HHmm}",
                new Dictionary<string, IReadOnlyList<string>>
                {
                    { "PNG изображение", [".png"] }
                },
                ".png");

            if (string.IsNullOrWhiteSpace(filePath))
            {
                ViewModel.StatusMessage = "Сохранение отменено.";
                return;
            }

            var exportChart = new SKCartesianChart(DepartmentHoursChart)
            {
                Width = Math.Max((int)DepartmentHoursChart.ActualWidth, 900),
                Height = Math.Max((int)DepartmentHoursChart.ActualHeight, 420)
            };

            exportChart.SaveImage(filePath, SKEncodedImageFormat.Png, 100);
            ViewModel.StatusMessage = "Файл сохранён.";
            await ExportSuccessDialogHelper.ShowAsync(App.MainWindow, filePath);
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Ошибка экспорта: {ex.Message}";
        }
    }
}
