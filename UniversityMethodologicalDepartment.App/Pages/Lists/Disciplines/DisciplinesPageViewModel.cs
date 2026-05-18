using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.ViewModels.Lists;

namespace UniversityMethodologicalDepartment.App.Pages.Lists.Disciplines;

public sealed class DisciplinesPageViewModel : ListPageViewModelBase<Discipline>
{
    public DisciplinesPageViewModel(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
        : base(dataService, appSettings, uiDispatcher)
    {
    }

    protected override EntitySet EntitySet => EntitySet.Disciplines;

    protected override Task<IReadOnlyList<Discipline>> LoadAllItemsAsync(CancellationToken cancellationToken)
        => DataService.GetDisciplinesAsync(cancellationToken: cancellationToken);

    protected override bool MatchesSearch(Discipline item, string searchText)
        => item.Name.Contains(searchText, System.StringComparison.OrdinalIgnoreCase);
}
