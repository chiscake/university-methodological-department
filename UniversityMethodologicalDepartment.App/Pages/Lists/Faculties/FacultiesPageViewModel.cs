using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.ViewModels.Lists;

namespace UniversityMethodologicalDepartment.App.Pages.Lists.Faculties;

public sealed class FacultiesPageViewModel : ListPageViewModelBase<Faculty>
{
    public FacultiesPageViewModel(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
        : base(dataService, appSettings, uiDispatcher)
    {
    }

    protected override EntitySet EntitySet => EntitySet.Faculties;

    protected override Task<IReadOnlyList<Faculty>> LoadAllItemsAsync(CancellationToken cancellationToken)
        => DataService.GetFacultiesAsync(cancellationToken);

    protected override bool MatchesSearch(Faculty item, string searchText)
        => item.Name.Contains(searchText, System.StringComparison.OrdinalIgnoreCase);
}
