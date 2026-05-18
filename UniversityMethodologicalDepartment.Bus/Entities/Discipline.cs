using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Дисциплина: название, кафедра
/// </summary>
public partial class Discipline
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<CurriculumItem> CurriculumItems { get; set; } = new List<CurriculumItem>();

    public virtual Department Department { get; set; } = null!;
}
