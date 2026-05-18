using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Pages.Admin;

public sealed partial class JournalPageViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IAuthService _authService;

    public JournalPageViewModel(IDataService dataService, IAuthService authService)
    {
        _dataService = dataService;
        _authService = authService;
    }

    public ObservableCollection<AuditLogListItem> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AuditLogListItem? SelectedItem { get; set; }

    public string DetailsOccurredAt => SelectedItem?.OccurredAt ?? "-";

    public string DetailsAction => SelectedItem?.Action ?? "-";

    public string DetailsTableName => SelectedItem?.TableName ?? "-";

    public string DetailsEntityId => SelectedItem?.EntityId ?? "-";

    public string DetailsActorUserId => SelectedItem?.ActorUserId ?? "-";

    public string DetailsOldRow => SelectedItem?.OldRowPretty ?? "-";

    public string DetailsNewRow => SelectedItem?.NewRowPretty ?? "-";

    partial void OnSelectedItemChanged(AuditLogListItem? value)
    {
        OnPropertyChanged(nameof(DetailsOccurredAt));
        OnPropertyChanged(nameof(DetailsAction));
        OnPropertyChanged(nameof(DetailsTableName));
        OnPropertyChanged(nameof(DetailsEntityId));
        OnPropertyChanged(nameof(DetailsActorUserId));
        OnPropertyChanged(nameof(DetailsOldRow));
        OnPropertyChanged(nameof(DetailsNewRow));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!_authService.IsAdmin)
        {
            Items.Clear();
            SelectedItem = null;
            StatusMessage = "Доступ только для администраторов.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var rows = await _dataService.GetAuditLogAsync(300).ConfigureAwait(true);
            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(new AuditLogListItem(
                    row.Id,
                    row.OccurredAt.ToLocalTime().ToString("g"),
                    ResolveActorDisplayName(row.ActorDisplayName, row.ActorUserId?.ToString()),
                    row.Action,
                    row.TableName,
                    row.EntityId ?? "-",
                    BuildPreview(row.OldRow),
                    BuildPreview(row.NewRow),
                    PrettyPrintJson(row.OldRow),
                    PrettyPrintJson(row.NewRow)));
            }

            SelectedItem = Items.Count > 0 ? Items[0] : null;

            if (Items.Count == 0)
            {
                StatusMessage = "Записи журнала пока отсутствуют.";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[JournalPageViewModel] Failed to load audit log: {ex}");
            Items.Clear();
            SelectedItem = null;
            StatusMessage = "Ошибка загрузки журнала.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string BuildPreview(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "-";
        }

        var compact = json.Replace(Environment.NewLine, " ", StringComparison.Ordinal).Trim();
        if (compact.Length <= 160)
        {
            return compact;
        }

        return compact[..160] + "...";
    }

    private static string PrettyPrintJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "-";
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            // Fallback to raw payload if audit value is not valid JSON.
            return json;
        }
    }

    private static string ResolveActorDisplayName(string? actorDisplayName, string? actorUserId)
    {
        if (!string.IsNullOrWhiteSpace(actorDisplayName))
        {
            return actorDisplayName;
        }

        if (!string.IsNullOrWhiteSpace(actorUserId))
        {
            return actorUserId;
        }

        return "-";
    }
}

public sealed record AuditLogListItem(
    long Id,
    string OccurredAt,
    string ActorUserId,
    string Action,
    string TableName,
    string EntityId,
    string OldRowPreview,
    string NewRowPreview,
    string OldRowPretty,
    string NewRowPretty);
