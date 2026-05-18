namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Сервис буфера обмена: копирование текста в системный буфер обмена.
/// </summary>
public interface IClipboardService
{
    /// <summary>Копирует указанный текст в буфер обмена.</summary>
    /// <param name="text">Текст для копирования.</param>
    void SetText(string text);
}
