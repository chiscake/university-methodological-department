namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Управление жизненным циклом приложения: завершение работы.
/// </summary>
public interface IAppLifecycle
{
    /// <summary>Завершает работу приложения.</summary>
    void Exit();
}
