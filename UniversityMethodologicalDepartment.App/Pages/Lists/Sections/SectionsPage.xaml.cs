using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Views.Details;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.App.Pages.Lists.Sections;

namespace UniversityMethodologicalDepartment.App.Pages.Lists;

public sealed partial class SectionPage : Page
{
    public SectionsPageViewModel ViewModel { get; }

    public SectionPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SectionsPageViewModel>();
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
        if (e.ClickedItem is Section section)
        {
            Debug.WriteLine($"[SectionPage] Item clicked: Id={section.Id}, Name={section.Name}.");
            App.MainWindow.Navigate(typeof(SectionDetailPage), new EntityDetailParameter(section.Id, section.Name));
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Navigate(typeof(SectionDetailPage), new EntityDetailParameter(0, "Новая секция", true));
    }
}
