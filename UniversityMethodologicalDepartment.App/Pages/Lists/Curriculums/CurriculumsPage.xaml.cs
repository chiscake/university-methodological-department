using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.Pages.Lists.Curriculums;
using UniversityMethodologicalDepartment.App.Models;

namespace UniversityMethodologicalDepartment.App.Pages.Lists;

public sealed partial class CurriculumsPage : Page
{
    public CurriculumsPageViewModel ViewModel { get; }

    public CurriculumsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CurriculumsPageViewModel>();
        DataContext = ViewModel;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Items.Count == 0 && !ViewModel.IsLoading)
        {
            _ = ViewModel.LoadAsync();
        }
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CurriculumItem curriculumItem)
        {
            Debug.WriteLine($"[CurriculumsPage] Item clicked: Id={curriculumItem.Id}, DisciplineId={curriculumItem.DisciplineId}, SpecialtyId={curriculumItem.SpecialtyId}.");
            var title = $"{curriculumItem.Discipline.Name} / {curriculumItem.Specialty.Name}";
            App.MainWindow.Navigate(typeof(CurriculumItemDetailPage), new EntityDetailParameter(curriculumItem.Id, title));
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Navigate(typeof(CurriculumItemDetailPage), new EntityDetailParameter(0, "Новый элемент учебного плана", true));
    }
}
