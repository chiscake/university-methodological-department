using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Helpers;

/// <summary>
/// Переходы на страницы деталей из ViewModel (аналогично спискам через <see cref="App.MainWindow"/>).
/// </summary>
internal static class DetailPageNavigator
{
    public static void ToEmployee(Employee? employee)
    {
        if (employee is null)
            return;

        var title = $"{employee.Surname} {employee.Name} {employee.Patronymic}".Trim();
        App.MainWindow.Navigate(typeof(EmployeeDetailPage), new EntityDetailParameter(employee.Id, title));
    }

    public static void ToFaculty(Faculty? faculty)
    {
        if (faculty is null)
            return;

        App.MainWindow.Navigate(typeof(FacultyDetailPage), new EntityDetailParameter(faculty.Id, faculty.Name ?? string.Empty));
    }

    public static void ToDepartment(Department? department)
    {
        if (department is null)
            return;

        App.MainWindow.Navigate(typeof(DepartmentDetailPage), new EntityDetailParameter(department.Id, department.Name ?? string.Empty));
    }

    public static void ToSection(Section? section)
    {
        if (section is null)
            return;

        App.MainWindow.Navigate(typeof(SectionDetailPage), new EntityDetailParameter(section.Id, section.Name ?? string.Empty));
    }

    public static void ToDiscipline(Discipline? discipline)
    {
        if (discipline is null)
            return;

        App.MainWindow.Navigate(typeof(DisciplineDetailPage), new EntityDetailParameter(discipline.Id, discipline.Name ?? string.Empty));
    }

    public static void ToSpecialty(Specialty? specialty)
    {
        if (specialty is null)
            return;

        App.MainWindow.Navigate(typeof(SpecialtyDetailPage), new EntityDetailParameter(specialty.Id, specialty.Name ?? string.Empty));
    }

    public static void ToCurriculumItem(CurriculumItem? item)
    {
        if (item is null)
            return;

        var spec = item.Specialty?.Name ?? string.Empty;
        var title = string.IsNullOrEmpty(spec)
            ? $"Семестр {item.Semester}"
            : $"{spec}, сем. {item.Semester}";
        App.MainWindow.Navigate(typeof(CurriculumItemDetailPage), new EntityDetailParameter(item.Id, title));
    }
}
