using System;
using Microsoft.UI.Xaml.Data;

namespace UniversityMethodologicalDepartment.App.Converters;

public sealed class NullOrWhitespaceToPlaceholderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var placeholder = parameter as string;
        if (string.IsNullOrWhiteSpace(placeholder))
        {
            placeholder = "Не указано";
        }

        if (value is null)
        {
            return placeholder;
        }

        if (value is string stringValue)
        {
            return string.IsNullOrWhiteSpace(stringValue) ? placeholder : stringValue;
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException("ConvertBack is not supported.");
    }
}
