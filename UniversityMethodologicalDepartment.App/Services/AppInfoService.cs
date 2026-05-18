using System;
using Helpers.Microsoft;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Предоставляет информацию о приложении: версия и сведения о среде выполнения WinAppSDK.
/// </summary>
public sealed class AppInfoService : IAppInfo
{
    /// <summary>Версия приложения в формате Major.Minor.Build.Revision.</summary>
    public string Version => ProcessInfoHelper.GetVersion() is Version version
        ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
        : string.Empty;

    /// <summary>Сведения о среде выполнения WinApp SDK.</summary>
    public string WinAppSdkRuntimeDetails => VersionHelper.WinAppSdkRuntimeDetails;
}
