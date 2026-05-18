using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Views.Details;

namespace UniversityMethodologicalDepartment.App.Helpers;

/// <summary>Навигация по выбранному результату глобального поиска.</summary>
internal static class ReferenceSearchNavigation
{
    public static void Open(ReferenceSearchItem item)
    {
        switch (item.Kind)
        {
            case ReferenceSearchKind.Faculty:
                App.MainWindow.Navigate(typeof(FacultyDetailPage), new EntityDetailParameter(item.Id, item.Title));
                break;
            case ReferenceSearchKind.Department:
                App.MainWindow.Navigate(typeof(DepartmentDetailPage), new EntityDetailParameter(item.Id, item.Title));
                break;
            case ReferenceSearchKind.Section:
                App.MainWindow.Navigate(typeof(SectionDetailPage), new EntityDetailParameter(item.Id, item.Title));
                break;
            case ReferenceSearchKind.Discipline:
                App.MainWindow.Navigate(typeof(DisciplineDetailPage), new EntityDetailParameter(item.Id, item.Title));
                break;
            case ReferenceSearchKind.Employee:
                App.MainWindow.Navigate(typeof(EmployeeDetailPage), new EntityDetailParameter(item.Id, item.Title));
                break;
            case ReferenceSearchKind.CurriculumItem:
                App.MainWindow.Navigate(typeof(CurriculumItemDetailPage), new EntityDetailParameter(item.Id, item.Title));
                break;
            case ReferenceSearchKind.Specialty:
                App.MainWindow.Navigate(typeof(SpecialtyDetailPage), new EntityDetailParameter(item.Id, item.Title));
                break;
        }
    }
}
