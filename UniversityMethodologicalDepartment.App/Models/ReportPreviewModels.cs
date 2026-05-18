using System.Collections.Generic;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Models;

public sealed record DepartmentSummaryPreviewRow(
    string FacultyName,
    string DepartmentName,
    string HeadFullName,
    int EmployeeCount,
    string? Phones,
    int DisciplineCount);

public sealed class SpecialtyOptionItem
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class MatrixPreviewRow
{
    public int SpecialtyId { get; init; }
    public string DisciplineLabel { get; init; } = string.Empty;
    public int LectureHours { get; init; }
    public int LabHours { get; init; }
    public int PracticalHours { get; init; }
    public int UsrHours { get; init; }
    public string CreditMark { get; init; } = string.Empty;
    public string ExamMark { get; init; } = string.Empty;
    public string CourseMark { get; init; } = string.Empty;
}

public sealed class WordDisciplinePreviewRow
{
    public string DisciplineName { get; init; } = string.Empty;
    public string Semesters { get; init; } = string.Empty;
    public string Specialties { get; init; } = string.Empty;
    public string ControlForms { get; init; } = string.Empty;
}

public sealed class WordDepartmentPreviewSection
{
    public string DepartmentName { get; init; } = string.Empty;
    public IReadOnlyList<WordDisciplinePreviewRow> Rows { get; init; } = [];
}

public sealed class ReportPreviewDocument
{
    public ReportExportKind Kind { get; init; }

    public IReadOnlyList<DepartmentSummaryPreviewRow>? DepartmentRows { get; init; }

    public IReadOnlyList<SpecialtyOptionItem>? SpecialtyOptions { get; init; }

    public IReadOnlyList<MatrixPreviewRow>? MatrixRows { get; init; }

    public IReadOnlyList<WordDepartmentPreviewSection>? WordSections { get; init; }
}
