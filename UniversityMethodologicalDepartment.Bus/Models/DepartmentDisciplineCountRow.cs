namespace UniversityMethodologicalDepartment.Bus.Models;

public sealed record DepartmentDisciplineCountRow(
    int DepartmentId,
    string DepartmentName,
    int DisciplineCount,
    string FacultyName,
    string HeadFullName,
    int EmployeeCount,
    string? Phones);
