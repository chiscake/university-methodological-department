using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Учёт дисциплины по специальности в семестре: часы, курсовой проект, экзамен/зачёт
/// </summary>
public partial class CurriculumItem
{
    public int Id { get; set; }

    public int DisciplineId { get; set; }

    public int SpecialtyId { get; set; }

    public int Semester { get; set; }

    public int LectureHours { get; set; }

    public int LabHours { get; set; }

    public int UsrHours { get; set; }

    public int PracticalHours { get; set; }

    public bool HasCourseProject { get; set; }

    public bool IsCredit { get; set; }

    public bool IsExam { get; set; }

    public virtual Discipline Discipline { get; set; } = null!;

    public virtual Specialty Specialty { get; set; } = null!;
}
