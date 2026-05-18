using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

public sealed class UiDispatcher : IUiDispatcher
{
    public Task EnqueueAsync(Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var queue = App.MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        if (queue is null)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue action to UI dispatcher queue."));
        }

        return tcs.Task;
    }

    public Task EnqueueAsync(Func<Task> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var queue = App.MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        if (queue is null)
            return action();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(async () =>
            {
                try
                {
                    await action().ConfigureAwait(true);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue action to UI dispatcher queue."));
        }

        return tcs.Task;
    }
}
