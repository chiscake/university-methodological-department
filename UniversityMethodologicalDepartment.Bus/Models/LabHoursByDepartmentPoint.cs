namespace UniversityMethodologicalDepartment.Bus.Models;

public sealed record LabHoursByDepartmentPoint(
    int DepartmentId,
    string DepartmentName,
    double TotalLabHours);
