using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.ViewModels.Lists;

namespace UniversityMethodologicalDepartment.App.Pages.Lists.Departments;

public sealed class DepartmentsPageViewModel : ListPageViewModelBase<Department>
{
    public DepartmentsPageViewModel(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
        : base(dataService, appSettings, uiDispatcher)
    {
    }

    protected override EntitySet EntitySet => EntitySet.Departments;

    protected override Task<IReadOnlyList<Department>> LoadAllItemsAsync(CancellationToken cancellationToken)
        => DataService.GetDepartmentsAsync(cancellationToken: cancellationToken);

    protected override bool MatchesSearch(Department item, string searchText)
        => item.Name.Contains(searchText, System.StringComparison.OrdinalIgnoreCase);
}
