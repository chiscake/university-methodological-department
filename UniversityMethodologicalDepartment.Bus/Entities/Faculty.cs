using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Факультет; деканом является один из сотрудников
/// </summary>
public partial class Faculty
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int DeanId { get; set; }

    public virtual Employee Dean { get; set; } = null!;

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
