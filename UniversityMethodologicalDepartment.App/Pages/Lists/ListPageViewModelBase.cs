using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.ViewModels.Lists;

public abstract partial class ListPageViewModelBase<T> : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IAppSettings _appSettings;
    private readonly IUiDispatcher _uiDispatcher;
    private bool _ignoreNextRefreshEvent;
    private List<T> _allItems = new();

    protected ListPageViewModelBase(IDataService dataService, IAppSettings appSettings, IUiDispatcher uiDispatcher)
    {
        _dataService = dataService;
        _appSettings = appSettings;
        _uiDispatcher = uiDispatcher;
        PageSize = NormalizePageSize(_appSettings.ListPageSize);
        _dataService.CacheRefreshed += OnCacheRefreshed;
    }

    protected IDataService DataService => _dataService;
    protected abstract EntitySet EntitySet { get; }

    public ObservableCollection<T> Items { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int PageSize { get; set; }

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DataStatus Status { get; set; } = DataStatus.Loading;

    public bool CanGoToPreviousPage => CurrentPage > 1;

    public bool CanGoToNextPage => CurrentPage < TotalPages;

    protected abstract Task<IReadOnlyList<T>> LoadAllItemsAsync(CancellationToken cancellationToken);

    protected abstract bool MatchesSearch(T item, string searchText);

    [RelayCommand]
    public async Task LoadAsync()
    {
        await LoadCoreAsync(isReloadAfterRefresh: false, CancellationToken.None).ConfigureAwait(true);
    }

    private async Task LoadCoreAsync(bool isReloadAfterRefresh, CancellationToken cancellationToken)
    {
        IsLoading = true;
        var hadCachedData = false;

        if (!isReloadAfterRefresh)
        {
            hadCachedData = await DataService.HasCachedDataAsync(EntitySet, cancellationToken).ConfigureAwait(true);
            Status = hadCachedData ? DataStatus.Cached : DataStatus.Loading;
        }

        StatusMessage = string.Empty;
        try
        {
            PageSize = NormalizePageSize(_appSettings.ListPageSize);
            var loaded = await LoadAllItemsAsync(cancellationToken).ConfigureAwait(true);
            _allItems = loaded.ToList();
            CurrentPage = 1;
            ApplyFilters();

            if (isReloadAfterRefresh || !hadCachedData)
                Status = DataStatus.Live;
        }
        catch
        {
            _allItems = new List<T>();
            Items.Clear();
            CurrentPage = 1;
            TotalPages = 1;
            TotalCount = 0;
            StatusMessage = "Ошибка загрузки";
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnCacheRefreshed(object? sender, CacheRefreshedEventArgs e)
    {
        if (e.EntitySet != EntitySet || IsLoading)
            return;

        if (_ignoreNextRefreshEvent)
        {
            _ignoreNextRefreshEvent = false;
            return;
        }

        try
        {
            _ignoreNextRefreshEvent = true;
            await _uiDispatcher.EnqueueAsync(async () =>
            {
                await LoadCoreAsync(isReloadAfterRefresh: true, CancellationToken.None).ConfigureAwait(true);
            }).ConfigureAwait(false);
        }
        catch
        {
            _ignoreNextRefreshEvent = false;
        }
    }

    [RelayCommand]
    private void Search()
    {
        CurrentPage = 1;
        ApplyFilters();
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage()
    {
        if (!CanGoToPreviousPage)
        {
            return;
        }

        CurrentPage--;
        ApplyFilters();
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (!CanGoToNextPage)
        {
            return;
        }

        CurrentPage++;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        PageSize = NormalizePageSize(_appSettings.ListPageSize);

        IEnumerable<T> query = _allItems;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(item => MatchesSearch(item, search));
        }

        var filtered = query.ToList();
        TotalCount = filtered.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }
        else if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        var skip = (CurrentPage - 1) * PageSize;
        var currentSlice = filtered.Skip(skip).Take(PageSize).ToList();

        Items.Clear();
        foreach (var item in currentSlice)
        {
            Items.Add(item);
        }

        StatusMessage = TotalCount == 0 ? "Ничего не найдено" : string.Empty;
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    private static int NormalizePageSize(int pageSize) => pageSize <= 0 ? 50 : pageSize;
}
