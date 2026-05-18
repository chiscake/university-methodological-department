namespace UniversityMethodologicalDepartment.Bus.Models;

public sealed record MultiSemesterDisciplineRow(
    string DisciplineName,
    int SemesterCount,
    string Semesters,
    string Departments);
