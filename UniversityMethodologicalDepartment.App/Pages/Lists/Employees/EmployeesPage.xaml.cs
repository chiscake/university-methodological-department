using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.Pages.Lists.Employees;

namespace UniversityMethodologicalDepartment.App.Pages.Lists;

public sealed partial class EmployeesPage : Page
{
    public EmployeesPageViewModel ViewModel { get; }

    public EmployeesPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<EmployeesPageViewModel>();
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
        if (e.ClickedItem is Employee employee)
        {
            Debug.WriteLine($"[EmployeesPage] Item clicked: Id={employee.Id}, Surname={employee.Surname}, Name={employee.Name}.");
            var title = $"{employee.Surname} {employee.Name} {employee.Patronymic}".Trim();
            App.MainWindow.Navigate(typeof(EmployeeDetailPage), new EntityDetailParameter(employee.Id, title));
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Navigate(typeof(EmployeeDetailPage), new EntityDetailParameter(0, "Новый сотрудник", true));
    }
}
