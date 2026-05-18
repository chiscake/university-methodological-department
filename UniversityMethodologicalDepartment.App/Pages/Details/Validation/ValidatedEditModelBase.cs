using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

namespace UniversityMethodologicalDepartment.App.ViewModels.Details.Validation;

/// <summary>
/// Базовый класс для editor-моделей, поддерживающий <see cref="INotifyDataErrorInfo"/>.
/// Используется DevWinUI <c>Validation</c> attached properties для отображения ошибок.
/// </summary>
public abstract partial class ValidatedEditModelBase : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _errors.Values.SelectMany(static list => list).ToList();
        }

        return _errors.TryGetValue(propertyName, out var errors)
            ? new List<string>(errors)
            : Array.Empty<string>();
    }

    /// <summary>
    /// Заменяет список ошибок для указанного свойства и поднимает <see cref="ErrorsChanged"/>.
    /// Также обновляет <see cref="HasErrors"/> при необходимости.
    /// </summary>
    protected void SetErrors(string propertyName, IList<string> errors)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            throw new ArgumentException("Property name must be non-empty.", nameof(propertyName));
        }

        var hadErrors = HasErrors;

        if (errors is null || errors.Count == 0)
        {
            if (!_errors.Remove(propertyName))
            {
                return;
            }
        }
        else
        {
            _errors[propertyName] = new List<string>(errors);
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        if (hadErrors != HasErrors)
        {
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
