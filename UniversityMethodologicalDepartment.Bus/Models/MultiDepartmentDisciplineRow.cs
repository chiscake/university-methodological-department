namespace UniversityMethodologicalDepartment.Bus.Models;

public sealed record MultiDepartmentDisciplineRow(
    string DisciplineName,
    int DepartmentCount,
    string Departments,
    int SemesterCount,
    int SpecialtyCount);
