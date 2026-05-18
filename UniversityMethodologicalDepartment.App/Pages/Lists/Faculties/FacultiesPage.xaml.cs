using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.Pages.Lists.Faculties;

namespace UniversityMethodologicalDepartment.App.Pages.Lists;

public sealed partial class FacultiesPage : Page
{
    public FacultiesPageViewModel ViewModel { get; }

    public FacultiesPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<FacultiesPageViewModel>();
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
        if (e.ClickedItem is Faculty faculty)
        {
            Debug.WriteLine($"[FacultiesPage] Item clicked: Id={faculty.Id}, Name={faculty.Name}.");
            App.MainWindow.Navigate(typeof(FacultyDetailPage), new EntityDetailParameter(faculty.Id, faculty.Name));
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Navigate(typeof(FacultyDetailPage), new EntityDetailParameter(0, "Новый факультет", true));
    }
}
