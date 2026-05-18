using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.Pages.Lists.Disciplines;

namespace UniversityMethodologicalDepartment.App.Pages.Lists;

public sealed partial class DisciplinesPage : Page
{
    public DisciplinesPageViewModel ViewModel { get; }

    public DisciplinesPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DisciplinesPageViewModel>();
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
        if (e.ClickedItem is Discipline discipline)
        {
            Debug.WriteLine($"[DisciplinesPage] Item clicked: Id={discipline.Id}, Name={discipline.Name}.");
            App.MainWindow.Navigate(typeof(DisciplineDetailPage), new EntityDetailParameter(discipline.Id, discipline.Name));
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Navigate(typeof(DisciplineDetailPage), new EntityDetailParameter(0, "Новая дисциплина", true));
    }
}
