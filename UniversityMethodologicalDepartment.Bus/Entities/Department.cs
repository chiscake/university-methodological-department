using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Кафедра: название, телефоны, факультет, заведующий
/// </summary>
public partial class Department
{
    public int Id { get; set; }

    public int FacultyId { get; set; }

    public int HeadId { get; set; }

    public string Name { get; set; } = null!;

    public string? Phones { get; set; }

    public virtual ICollection<Discipline> Disciplines { get; set; } = new List<Discipline>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual Faculty Faculty { get; set; } = null!;

    public virtual Employee Head { get; set; } = null!;

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}
