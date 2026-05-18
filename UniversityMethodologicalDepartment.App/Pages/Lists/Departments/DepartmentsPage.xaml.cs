using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.Pages.Lists.Departments;

namespace UniversityMethodologicalDepartment.App.Pages.Lists;

public sealed partial class DepartmentsPage : Page
{
    public DepartmentsPageViewModel ViewModel { get; }

    public DepartmentsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DepartmentsPageViewModel>();
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
        if (e.ClickedItem is Department department)
        {
            Debug.WriteLine($"[DepartmentsPage] Item clicked: Id={department.Id}, Name={department.Name}.");
            App.MainWindow.Navigate(typeof(DepartmentDetailPage), new EntityDetailParameter(department.Id, department.Name));
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Navigate(typeof(DepartmentDetailPage), new EntityDetailParameter(0, "Новая кафедра", true));
    }
}
