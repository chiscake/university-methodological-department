using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Специальность: код, название, квалификация, продолжительность, форма обучения
/// </summary>
public partial class Specialty
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Qualification { get; set; }

    public int? Duration { get; set; }

    public string? FormOfStudy { get; set; }

    public virtual ICollection<CurriculumItem> CurriculumItems { get; set; } = new List<CurriculumItem>();
}
