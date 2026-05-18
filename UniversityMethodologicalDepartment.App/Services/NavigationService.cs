using System;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Реализация навигации через <see cref="Frame"/> WinUI с использованием <see cref="IServiceProvider"/> для резолва зависимостей.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Frame _frame;
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(Frame frame, IServiceProvider serviceProvider)
    {
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Провайдер сервисов для резолва зависимостей (например, ViewModel'ов на страницах).
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    /// <inheritdoc />
    public void Navigate(Type pageType, object? parameter = null)
    {
        _frame.Navigate(pageType, parameter);
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (_frame.CanGoBack)
            _frame.GoBack();
    }

    /// <inheritdoc />
    public bool CanGoBack => _frame.CanGoBack;
}
