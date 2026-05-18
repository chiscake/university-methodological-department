using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using UniversityMethodologicalDepartment.App.ViewModels.Details;
using UniversityMethodologicalDepartment.App.Models;

namespace UniversityMethodologicalDepartment.App.Views.Details;

public sealed partial class DepartmentDetailPage : Page
{
    public DepartmentDetailPageViewModel ViewModel { get; }

    public DepartmentDetailPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DepartmentDetailPageViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is EntityDetailParameter parameter)
        {
            ViewModel.Initialize(parameter);
            if (parameter.IsCreateMode)
            {
                ViewModel.OpenEditorCommand.Execute(null);
            }
            else
            {
                _ = ViewModel.LoadAsync();
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Dispose();
    }

    private async void HeadAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        await ViewModel.UpdateHeadSuggestionsAsync(sender.Text).ConfigureAwait(true);
    }

    private void HeadAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ReferenceSearchItem item)
        {
            ViewModel.SelectHeadSuggestion(item);
            sender.Text = item.Title;
        }
    }

    private void HeadAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is ReferenceSearchItem item)
        {
            ViewModel.SelectHeadSuggestion(item);
            sender.Text = item.Title;
            return;
        }

        ViewModel.SelectHeadFirstSuggestion();
    }
}
