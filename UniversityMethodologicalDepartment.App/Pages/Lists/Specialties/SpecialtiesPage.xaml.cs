using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Pages.Lists.Specialties;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Pages.Lists;

public sealed partial class SpecialtiesPage : Page
{
    public SpecialtiesPageViewModel ViewModel { get; }

    public SpecialtiesPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SpecialtiesPageViewModel>();
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
        if (e.ClickedItem is Specialty specialty)
        {
            Debug.WriteLine($"[SpecialtiesPage] Item clicked: Id={specialty.Id}, Name={specialty.Name}.");
            App.MainWindow.Navigate(typeof(SpecialtyDetailPage), new EntityDetailParameter(specialty.Id, specialty.Name));
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Navigate(typeof(SpecialtyDetailPage), new EntityDetailParameter(0, "Новая специальность", true));
    }
}
