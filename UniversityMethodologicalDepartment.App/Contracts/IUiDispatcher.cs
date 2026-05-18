using System;
using System.Threading.Tasks;

namespace UniversityMethodologicalDepartment.App.Contracts;

public interface IUiDispatcher
{
    Task EnqueueAsync(Action action);

    Task EnqueueAsync(Func<Task> action);
}
