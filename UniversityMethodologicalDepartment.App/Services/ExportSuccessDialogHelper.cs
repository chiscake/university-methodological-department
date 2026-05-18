using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UniversityMethodologicalDepartment.App.Services;

internal static class ExportSuccessDialogHelper
{
    public static async Task ShowAsync(Window owner, string filePath)
    {
        var dialog = new ContentDialog
        {
            Title = "Успешно сохранено",
            Content = filePath,
            PrimaryButtonText = "Открыть файл",
            SecondaryButtonText = "Показать в папке",
            CloseButtonText = "ОК",
            XamlRoot = owner.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        switch (result)
        {
            case ContentDialogResult.Primary:
                OpenWithExplorer(filePath);
                break;
            case ContentDialogResult.Secondary:
                ShowInExplorer(filePath);
                break;
            default:
                break;
        }
    }

    private static void OpenWithExplorer(string filePath)
    {
        var startInfo = new ProcessStartInfo("explorer.exe", $"\"{filePath}\"")
        {
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }

    private static void ShowInExplorer(string filePath)
    {
        var startInfo = new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
        {
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }
}
