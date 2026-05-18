using System;
using Microsoft.UI.Xaml.Data;

namespace UniversityMethodologicalDepartment.App.Converters;

public sealed class BooleanToYesNoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue && boolValue ? "Да" : "Нет";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string stringValue)
        {
            if (string.Equals(stringValue, "Да", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(stringValue, "Нет", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        throw new NotSupportedException("ConvertBack is not supported.");
    }
}
