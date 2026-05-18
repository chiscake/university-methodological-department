using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace UniversityMethodologicalDepartment.App.Converters;

/// <summary>
/// Преобразует значение в Visibility:
/// - null → Collapsed
/// - пустая или состоящая из пробелов строка → Collapsed
/// - иначе → Visible.
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null)
            return Visibility.Collapsed;

        if (value is string s && string.IsNullOrWhiteSpace(s))
            return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("NullToVisibilityConverter.ConvertBack не поддерживается.");
    }
}
