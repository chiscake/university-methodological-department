using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UniversityMethodologicalDepartment.App.Controls;

public sealed partial class DetailEditorHost : UserControl
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(DetailEditorHost),
            new PropertyMetadata(false));

    public static readonly DependencyProperty CanSaveProperty =
        DependencyProperty.Register(
            nameof(CanSave),
            typeof(bool),
            typeof(DetailEditorHost),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(
            nameof(IsBusy),
            typeof(bool),
            typeof(DetailEditorHost),
            new PropertyMetadata(false));

    public static readonly DependencyProperty SaveCommandProperty =
        DependencyProperty.Register(
            nameof(SaveCommand),
            typeof(ICommand),
            typeof(DetailEditorHost),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(
            nameof(CancelCommand),
            typeof(ICommand),
            typeof(DetailEditorHost),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(DetailEditorHost),
            new PropertyMetadata("Редактирование"));

    public static readonly DependencyProperty EditorContentTemplateProperty =
        DependencyProperty.Register(
            nameof(EditorContentTemplate),
            typeof(DataTemplate),
            typeof(DetailEditorHost),
            new PropertyMetadata(null));

    public static readonly DependencyProperty EditorContentProperty =
        DependencyProperty.Register(
            nameof(EditorContent),
            typeof(object),
            typeof(DetailEditorHost),
            new PropertyMetadata(null));

    public DetailEditorHost()
    {
        InitializeComponent();
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool CanSave
    {
        get => (bool)GetValue(CanSaveProperty);
        set => SetValue(CanSaveProperty, value);
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public ICommand? SaveCommand
    {
        get => (ICommand?)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public DataTemplate? EditorContentTemplate
    {
        get => (DataTemplate?)GetValue(EditorContentTemplateProperty);
        set => SetValue(EditorContentTemplateProperty, value);
    }

    public object? EditorContent
    {
        get => GetValue(EditorContentProperty);
        set => SetValue(EditorContentProperty, value);
    }
}
