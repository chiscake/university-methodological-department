using Microsoft.UI.Xaml;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Управление жизненным циклом приложения: завершение работы.
/// </summary>
public sealed class AppLifecycleService : IAppLifecycle
{
    /// <summary>Завершает работу приложения.</summary>
    public void Exit()
    {
        Application.Current.Exit();
    }
}
