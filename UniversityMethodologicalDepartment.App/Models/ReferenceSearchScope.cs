using System;

namespace UniversityMethodologicalDepartment.App.Models;

[Flags]
public enum ReferenceSearchScope
{
    None = 0,
    Faculty = 1 << 0,
    Department = 1 << 1,
    Section = 1 << 2,
    Discipline = 1 << 3,
    Employee = 1 << 4,
    CurriculumItem = 1 << 5,
    Specialty = 1 << 6,
    All = Faculty | Department | Section | Discipline | Employee | CurriculumItem | Specialty
}
