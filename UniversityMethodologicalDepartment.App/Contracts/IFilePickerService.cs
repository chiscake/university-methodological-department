using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityMethodologicalDepartment.App.Contracts;

/// <summary>
/// Сервис диалогов выбора и сохранения файлов (пикеры).
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Показать диалог выбора одного файла для открытия.
    /// </summary>
    /// <param name="fileTypeFilter">Расширения файлов (например, ".txt", ".dat").</param>
    /// <param name="commitButtonText">Текст кнопки подтверждения.</param>
    /// <returns>Путь к выбранному файлу или null при отмене.</returns>
    Task<string?> PickOpenFileAsync(
        IReadOnlyList<string> fileTypeFilter,
        string commitButtonText = "Открыть");

    /// <summary>
    /// Показать диалог сохранения файла.
    /// </summary>
    /// <param name="suggestedFileName">Предлагаемое имя файла.</param>
    /// <param name="fileTypeChoices">Группы расширений: отображаемое имя → список расширений.</param>
    /// <param name="defaultExtension">Расширение по умолчанию (например, ".txt").</param>
    /// <param name="commitButtonText">Текст кнопки подтверждения.</param>
    /// <returns>Путь к выбранному файлу или null при отмене.</returns>
    Task<string?> PickSaveFileAsync(
        string suggestedFileName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> fileTypeChoices,
        string defaultExtension,
        string commitButtonText = "Сохранить");
}
