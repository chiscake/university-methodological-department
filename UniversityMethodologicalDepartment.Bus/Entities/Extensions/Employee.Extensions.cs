using System.Linq;

namespace UniversityMethodologicalDepartment.Bus.Entities;

public partial class Employee
{
    public string FullName =>
        string.Join(" ", new[] { Surname, Name, Patronymic }.Where(static s => !string.IsNullOrWhiteSpace(s)));
}
