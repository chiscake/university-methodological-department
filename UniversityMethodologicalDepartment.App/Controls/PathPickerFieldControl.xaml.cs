using System.Windows.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UniversityMethodologicalDepartment.App.Controls;

public sealed record CreateFileDefaults(string SuggestedFileName, string DefaultExtension);

public sealed partial class PathPickerFieldControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PathTextProperty =
        DependencyProperty.Register(
            nameof(PathText),
            typeof(string),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PickButtonTextProperty =
        DependencyProperty.Register(
            nameof(PickButtonText),
            typeof(string),
            typeof(PathPickerFieldControl),
            new PropertyMetadata("Выбрать"));

    public static readonly DependencyProperty PickCommandProperty =
        DependencyProperty.Register(
            nameof(PickCommand),
            typeof(ICommand),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CreateButtonTextProperty =
        DependencyProperty.Register(
            nameof(CreateButtonText),
            typeof(string),
            typeof(PathPickerFieldControl),
            new PropertyMetadata("Создать"));

    public static readonly DependencyProperty CreateCommandProperty =
        DependencyProperty.Register(
            nameof(CreateCommand),
            typeof(ICommand),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CreateFileNameDefaultProperty =
        DependencyProperty.Register(
            nameof(CreateFileNameDefault),
            typeof(string),
            typeof(PathPickerFieldControl),
            new PropertyMetadata("file", OnCreateDefaultsChanged));

    public static readonly DependencyProperty CreateFileExtensionDefaultProperty =
        DependencyProperty.Register(
            nameof(CreateFileExtensionDefault),
            typeof(string),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(".txt", OnCreateDefaultsChanged));

    public static readonly DependencyProperty CreateCommandParameterProperty =
        DependencyProperty.Register(
            nameof(CreateCommandParameter),
            typeof(CreateFileDefaults),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PathBoxWidthProperty =
        DependencyProperty.Register(
            nameof(PathBoxWidth),
            typeof(double),
            typeof(PathPickerFieldControl),
            new PropertyMetadata(double.NaN));

    public PathPickerFieldControl()
    {
        InitializeComponent();
        UpdateCreateCommandParameter();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string PathText
    {
        get => (string)GetValue(PathTextProperty);
        set => SetValue(PathTextProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public string PickButtonText
    {
        get => (string)GetValue(PickButtonTextProperty);
        set => SetValue(PickButtonTextProperty, value);
    }

    public ICommand? PickCommand
    {
        get => (ICommand?)GetValue(PickCommandProperty);
        set => SetValue(PickCommandProperty, value);
    }

    public string CreateButtonText
    {
        get => (string)GetValue(CreateButtonTextProperty);
        set => SetValue(CreateButtonTextProperty, value);
    }

    public ICommand? CreateCommand
    {
        get => (ICommand?)GetValue(CreateCommandProperty);
        set => SetValue(CreateCommandProperty, value);
    }

    public string CreateFileNameDefault
    {
        get => (string)GetValue(CreateFileNameDefaultProperty);
        set => SetValue(CreateFileNameDefaultProperty, value);
    }

    public string CreateFileExtensionDefault
    {
        get => (string)GetValue(CreateFileExtensionDefaultProperty);
        set => SetValue(CreateFileExtensionDefaultProperty, value);
    }

    public CreateFileDefaults? CreateCommandParameter
    {
        get => (CreateFileDefaults?)GetValue(CreateCommandParameterProperty);
        private set => SetValue(CreateCommandParameterProperty, value);
    }

    public double PathBoxWidth
    {
        get => (double)GetValue(PathBoxWidthProperty);
        set => SetValue(PathBoxWidthProperty, value);
    }

    private static void OnCreateDefaultsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs _)
    {
        if (dependencyObject is PathPickerFieldControl control)
            control.UpdateCreateCommandParameter();
    }

    private void UpdateCreateCommandParameter()
    {
        string name = string.IsNullOrWhiteSpace(CreateFileNameDefault) ? "file" : CreateFileNameDefault;
        string extension = string.IsNullOrWhiteSpace(CreateFileExtensionDefault) ? ".txt" : CreateFileExtensionDefault;
        CreateCommandParameter = new CreateFileDefaults(name, extension);
    }
}
