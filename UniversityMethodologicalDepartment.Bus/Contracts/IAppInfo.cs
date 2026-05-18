namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Информация о приложении: версия и сведения о среде выполнения.
/// </summary>
public interface IAppInfo
{
    /// <summary>Версия приложения.</summary>
    string Version { get; }

    /// <summary>Сведения о среде выполнения WinApp SDK.</summary>
    string WinAppSdkRuntimeDetails { get; }
}
