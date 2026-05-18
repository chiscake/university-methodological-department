using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.ViewModels.Lists;

namespace UniversityMethodologicalDepartment.App.Pages.Lists.Sections;

public sealed class SectionsPageViewModel : ListPageViewModelBase<Section>
{
    public SectionsPageViewModel(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
        : base(dataService, appSettings, uiDispatcher)
    {
    }

    protected override EntitySet EntitySet => EntitySet.Sections;

    protected override Task<IReadOnlyList<Section>> LoadAllItemsAsync(CancellationToken cancellationToken)
        => DataService.GetSectionsAsync(cancellationToken: cancellationToken);

    protected override bool MatchesSearch(Section item, string searchText)
        => item.Name.Contains(searchText, System.StringComparison.OrdinalIgnoreCase);
}
