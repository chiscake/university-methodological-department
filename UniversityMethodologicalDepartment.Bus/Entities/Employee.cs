using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Сотрудники: ФИО, учёная степень, звание, контакты
/// </summary>
public partial class Employee
{
    public int Id { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Patronymic { get; set; }

    public string? Degree { get; set; }

    public string? Title { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int? DepartmentId { get; set; }

    /// <summary>
    /// Секция в рамках кафедры (department_id); опционально
    /// </summary>
    public int? SectionId { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();

    public virtual Section? Section { get; set; }

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}
