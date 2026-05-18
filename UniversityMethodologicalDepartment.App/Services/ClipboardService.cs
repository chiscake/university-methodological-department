using UniversityMethodologicalDepartment.App.Bus.Contracts;
using Windows.ApplicationModel.DataTransfer;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Сервис буфера обмена: копирование текста в системный буфер обмена.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    /// <summary>Копирует указанный текст в буфер обмена.</summary>
    /// <param name="text">Текст для копирования.</param>
    public void SetText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }
}
