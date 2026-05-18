using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using UniversityMethodologicalDepartment.App.ViewModels.Details;
using UniversityMethodologicalDepartment.App.Models;

namespace UniversityMethodologicalDepartment.App.Views.Details;

public sealed partial class EmployeeDetailPage : Page
{
    public EmployeeDetailPageViewModel ViewModel { get; }

    public EmployeeDetailPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<EmployeeDetailPageViewModel>();
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

    private async void DepartmentAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        await ViewModel.UpdateDepartmentSuggestionsAsync(sender.Text).ConfigureAwait(true);
    }

    private void DepartmentAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ReferenceSearchItem item)
        {
            ViewModel.SelectDepartmentSuggestion(item);
            sender.Text = item.Title;
        }
    }

    private void DepartmentAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is ReferenceSearchItem item)
        {
            ViewModel.SelectDepartmentSuggestion(item);
            sender.Text = item.Title;
            return;
        }

        ViewModel.SelectDepartmentFirstSuggestion();
    }

    private async void SectionAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        await ViewModel.UpdateSectionSuggestionsAsync(sender.Text).ConfigureAwait(true);
    }

    private void SectionAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ReferenceSearchItem item)
        {
            ViewModel.SelectSectionSuggestion(item);
            sender.Text = item.Title;
        }
    }

    private void SectionAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is ReferenceSearchItem item)
        {
            ViewModel.SelectSectionSuggestion(item);
            sender.Text = item.Title;
            return;
        }

        ViewModel.SelectSectionFirstSuggestion();
    }
}
