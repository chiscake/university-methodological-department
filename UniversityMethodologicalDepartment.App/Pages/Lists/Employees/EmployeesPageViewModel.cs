using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.ViewModels.Lists;

namespace UniversityMethodologicalDepartment.App.Pages.Lists.Employees;

public sealed class EmployeesPageViewModel : ListPageViewModelBase<Employee>
{
    public EmployeesPageViewModel(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
        : base(dataService, appSettings, uiDispatcher)
    {
    }

    protected override EntitySet EntitySet => EntitySet.Employees;

    protected override Task<IReadOnlyList<Employee>> LoadAllItemsAsync(CancellationToken cancellationToken)
        => DataService.GetEmployeesAsync(cancellationToken: cancellationToken);

    protected override bool MatchesSearch(Employee item, string searchText)
    {
        return item.FullName.Contains(searchText, System.StringComparison.OrdinalIgnoreCase);
    }
}
