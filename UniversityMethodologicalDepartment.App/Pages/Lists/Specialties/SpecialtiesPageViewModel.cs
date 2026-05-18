using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.ViewModels.Lists;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Pages.Lists.Specialties;

public sealed class SpecialtiesPageViewModel : ListPageViewModelBase<Specialty>
{
    public SpecialtiesPageViewModel(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
        : base(dataService, appSettings, uiDispatcher)
    {
    }

    protected override EntitySet EntitySet => EntitySet.Specialties;

    protected override Task<IReadOnlyList<Specialty>> LoadAllItemsAsync(CancellationToken cancellationToken)
        => DataService.GetSpecialtiesAsync(cancellationToken);

    protected override bool MatchesSearch(Specialty item, string searchText)
        => (item.Code?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.Qualification?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.FormOfStudy?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
}
