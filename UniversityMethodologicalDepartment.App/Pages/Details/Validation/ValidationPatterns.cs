using System.Text.RegularExpressions;

namespace UniversityMethodologicalDepartment.App.ViewModels.Details.Validation;

/// <summary>
/// Регулярные выражения, используемые в editor-моделях для проверки отдельных полей.
/// </summary>
internal static partial class ValidationPatterns
{
    [GeneratedRegex(@"^[\d+\-()\s,]+$", RegexOptions.CultureInvariant)]
    public static partial Regex PhonesRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    public static partial Regex EmailRegex();
}
