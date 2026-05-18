using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Windows.Storage.Pickers;

using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

internal sealed class FilePickerService : IFilePickerService
{
    private static Microsoft.UI.WindowId GetWindowId()
    {
        var window = App.MainWindow;
        if (window?.AppWindow == null)
            throw new InvalidOperationException("MainWindow or AppWindow is not available.");
        return window.AppWindow.Id;
    }

    public async Task<string?> PickOpenFileAsync(
        IReadOnlyList<string> fileTypeFilter,
        string commitButtonText = "Открыть")
    {
        var windowId = GetWindowId();
        var openPicker = new FileOpenPicker(windowId)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            CommitButtonText = commitButtonText,
            ViewMode = PickerViewMode.List,
        };

        foreach (var ext in fileTypeFilter)
            openPicker.FileTypeFilter.Add(ext);

        var result = await openPicker.PickSingleFileAsync().AsTask();
        return result?.Path;
    }

    public async Task<string?> PickSaveFileAsync(
        string suggestedFileName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> fileTypeChoices,
        string defaultExtension,
        string commitButtonText = "Сохранить")
    {
        var windowId = GetWindowId();
        var savePicker = new FileSavePicker(windowId)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = defaultExtension,
            CommitButtonText = commitButtonText,
        };

        foreach (var (label, extensions) in fileTypeChoices)
        {
            var list = extensions.ToList();
            savePicker.FileTypeChoices.Add(label, list);
        }

        var result = await savePicker.PickSaveFileAsync().AsTask();
        return result?.Path;
    }
}
