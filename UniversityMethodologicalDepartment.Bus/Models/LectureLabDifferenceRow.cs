namespace UniversityMethodologicalDepartment.Bus.Models;

public sealed record LectureLabDifferenceRow(
    int DisciplineId,
    string DisciplineName,
    int LectureHours,
    int LabHours,
    int DifferenceHours);
