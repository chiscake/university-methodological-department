using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.ViewModels.Lists;

namespace UniversityMethodologicalDepartment.App.Pages.Lists.Curriculums;

public sealed class CurriculumsPageViewModel : ListPageViewModelBase<CurriculumItem>
{
    public CurriculumsPageViewModel(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
        : base(dataService, appSettings, uiDispatcher)
    {
    }

    protected override EntitySet EntitySet => EntitySet.CurriculumItems;

    protected override Task<IReadOnlyList<CurriculumItem>> LoadAllItemsAsync(CancellationToken cancellationToken)
        => DataService.GetCurriculumItemsAsync(cancellationToken: cancellationToken);

    protected override bool MatchesSearch(CurriculumItem item, string searchText)
        => item.Discipline.Name.Contains(searchText, System.StringComparison.OrdinalIgnoreCase)
           || item.Specialty.Name.Contains(searchText, System.StringComparison.OrdinalIgnoreCase);
}
