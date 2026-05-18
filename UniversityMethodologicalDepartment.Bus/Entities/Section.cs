using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Предметно-методическая секция кафедры: название, руководитель (опционально), контакты
/// </summary>
public partial class Section
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }

    public string Name { get; set; } = null!;

    public int? HeadId { get; set; }

    public string? Phones { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual Employee? Head { get; set; }
}
