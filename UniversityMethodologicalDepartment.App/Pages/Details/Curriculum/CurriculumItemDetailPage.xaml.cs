using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using UniversityMethodologicalDepartment.App.ViewModels.Details;
using UniversityMethodologicalDepartment.App.Models;

namespace UniversityMethodologicalDepartment.App.Views.Details;

public sealed partial class CurriculumItemDetailPage : Page
{
    public CurriculumItemDetailPageViewModel ViewModel { get; }

    public CurriculumItemDetailPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CurriculumItemDetailPageViewModel>();
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

    private async void DisciplineAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        ViewModel.HandleDisciplineQueryUserInput(sender.Text);
        await ViewModel.UpdateDisciplineSuggestionsAsync(sender.Text).ConfigureAwait(true);
    }

    private void DisciplineAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ReferenceSearchItem item)
        {
            ViewModel.SelectDisciplineSuggestion(item);
            sender.Text = item.Title;
        }
    }

    private void DisciplineAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is ReferenceSearchItem item)
        {
            ViewModel.SelectDisciplineSuggestion(item);
            sender.Text = item.Title;
            return;
        }

        ViewModel.SelectDisciplineFirstSuggestion();
    }

    private async void SpecialtyAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        ViewModel.HandleSpecialtyQueryUserInput(sender.Text);
        await ViewModel.UpdateSpecialtySuggestionsAsync(sender.Text).ConfigureAwait(true);
    }

    private void SpecialtyAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ReferenceSearchItem item)
        {
            ViewModel.SelectSpecialtySuggestion(item);
            sender.Text = item.Title;
        }
    }

    private void SpecialtyAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is ReferenceSearchItem item)
        {
            ViewModel.SelectSpecialtySuggestion(item);
            sender.Text = item.Title;
            return;
        }

        ViewModel.SelectSpecialtyFirstSuggestion();
    }
}
