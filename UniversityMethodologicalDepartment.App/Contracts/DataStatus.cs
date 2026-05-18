using System.ComponentModel.DataAnnotations;

namespace UniversityMethodologicalDepartment.App.Contracts;

public enum DataStatus
{
    [Display(Name = "Актуально")]
    Live,

    [Display(Name = "Офлайн копия")]
    Cached,

    [Display(Name = "Загрузка")]
    Loading
}
