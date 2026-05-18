using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using UniversityMethodologicalDepartment.App.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.ApplicationModel.DataTransfer;

namespace UniversityMethodologicalDepartment.App.Controls;

/// <summary>Выравнивание и порядок кнопок в панели.</summary>
public enum ButtonsAlignment
{
    /// <summary>Слева: [ Сохранить ] [ Загрузить ] [ Копировать ].</summary>
    Left,

    /// <summary>Справа: [ Копировать ] [ Загрузить ] [ Сохранить ].</summary>
    Right
}

public sealed partial class FileContentBoxControl : UserControl
{
    private const string DefaultOpenCommitText = "Выбрать файл";
    private const string DefaultSaveCommitText = "Сохранить в файл";
    private const string AccentButtonStyleKey = "AccentButtonStyle";

    private static readonly IReadOnlyList<string> OpenFileTypes = [".txt"];
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SaveFileTypes =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Текстовые файлы (*.txt)"] = [".txt"]
        };

    private string _baselineText = string.Empty;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(FileContentBoxControl),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(FileContentBoxControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LastFilePathProperty =
        DependencyProperty.Register(
            nameof(LastFilePath),
            typeof(string),
            typeof(FileContentBoxControl),
            new PropertyMetadata(string.Empty, OnLastFilePathPropertyChanged));

    public static readonly DependencyProperty IsModifiedProperty =
        DependencyProperty.Register(
            nameof(IsModified),
            typeof(bool),
            typeof(FileContentBoxControl),
            new PropertyMetadata(false, OnIsModifiedPropertyChanged));

    public static readonly DependencyProperty IsLoadFromFileEnabledProperty =
        DependencyProperty.Register(
            nameof(IsLoadFromFileEnabled),
            typeof(bool),
            typeof(FileContentBoxControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsSaveEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSaveEnabled),
            typeof(bool),
            typeof(FileContentBoxControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ContentHeightProperty =
        DependencyProperty.Register(
            nameof(ContentHeight),
            typeof(double),
            typeof(FileContentBoxControl),
            new PropertyMetadata(120.0));

    public static readonly DependencyProperty IsCopyEnabledProperty =
        DependencyProperty.Register(
            nameof(IsCopyEnabled),
            typeof(bool),
            typeof(FileContentBoxControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ButtonsAlignmentProperty =
        DependencyProperty.Register(
            nameof(ButtonsAlignment),
            typeof(ButtonsAlignment),
            typeof(FileContentBoxControl),
            new PropertyMetadata(ButtonsAlignment.Left, OnButtonsAlignmentPropertyChanged));

    public static readonly DependencyProperty SaveButtonUsesAccentStyleProperty =
        DependencyProperty.Register(
            nameof(SaveButtonUsesAccentStyle),
            typeof(bool),
            typeof(FileContentBoxControl),
            new PropertyMetadata(false, OnSaveButtonUsesAccentStylePropertyChanged));

    public static readonly DependencyProperty IsFileDisplayVisibleProperty =
        DependencyProperty.Register(
            nameof(IsFileDisplayVisible),
            typeof(bool),
            typeof(FileContentBoxControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsModifiedDisplayVisibleProperty =
        DependencyProperty.Register(
            nameof(IsModifiedDisplayVisible),
            typeof(bool),
            typeof(FileContentBoxControl),
            new PropertyMetadata(true, OnIsModifiedDisplayVisiblePropertyChanged));

    private static readonly DependencyProperty LastFileDisplayNameProperty =
        DependencyProperty.Register(
            nameof(LastFileDisplayName),
            typeof(string),
            typeof(FileContentBoxControl),
            new PropertyMetadata("Файл не выбран"));

    private static readonly DependencyProperty IsModifiedDisplayTextProperty =
        DependencyProperty.Register(
            nameof(IsModifiedDisplayText),
            typeof(string),
            typeof(FileContentBoxControl),
            new PropertyMetadata(string.Empty));

    private static readonly DependencyProperty IsModifiedVisibilityProperty =
        DependencyProperty.Register(
            nameof(IsModifiedVisibility),
            typeof(Visibility),
            typeof(FileContentBoxControl),
            new PropertyMetadata(Visibility.Collapsed));

    private static readonly DependencyProperty EffectiveModifiedVisibilityProperty =
        DependencyProperty.Register(
            nameof(EffectiveModifiedVisibility),
            typeof(Visibility),
            typeof(FileContentBoxControl),
            new PropertyMetadata(Visibility.Collapsed));

    public FileContentBoxControl()
    {
        InitializeComponent();
        LoadFileCommand = new AsyncRelayCommand(LoadFileAsync);
        SaveFileCommand = new AsyncRelayCommand(SaveFileAsync);
        CopyCommand = new RelayCommand(CopyToClipboard);
        ApplyButtonsAlignment();
        ApplySaveButtonAccentStyle();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string LastFilePath
    {
        get => (string)GetValue(LastFilePathProperty);
        set => SetValue(LastFilePathProperty, value);
    }

    public bool IsModified
    {
        get => (bool)GetValue(IsModifiedProperty);
        set => SetValue(IsModifiedProperty, value);
    }

    /// <summary>Показывать ли кнопку «Загрузить из файла» (по умолчанию true).</summary>
    public bool IsLoadFromFileEnabled
    {
        get => (bool)GetValue(IsLoadFromFileEnabledProperty);
        set => SetValue(IsLoadFromFileEnabledProperty, value);
    }

    /// <summary>Показывать ли кнопку «Сохранить в файл» (по умолчанию true).</summary>
    public bool IsSaveEnabled
    {
        get => (bool)GetValue(IsSaveEnabledProperty);
        set => SetValue(IsSaveEnabledProperty, value);
    }

    /// <summary>Высота текстбокса ввода (по умолчанию 120).</summary>
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }

    /// <summary>Показывать ли кнопку «Копировать» (по умолчанию true).</summary>
    public bool IsCopyEnabled
    {
        get => (bool)GetValue(IsCopyEnabledProperty);
        set => SetValue(IsCopyEnabledProperty, value);
    }

    /// <summary>Выравнивание и порядок кнопок (по умолчанию Left).</summary>
    public ButtonsAlignment ButtonsAlignment
    {
        get => (ButtonsAlignment)GetValue(ButtonsAlignmentProperty);
        set => SetValue(ButtonsAlignmentProperty, value);
    }

    /// <summary>Применить акцентный стиль к кнопке «Сохранить» (по умолчанию false).</summary>
    public bool SaveButtonUsesAccentStyle
    {
        get => (bool)GetValue(SaveButtonUsesAccentStyleProperty);
        set => SetValue(SaveButtonUsesAccentStyleProperty, value);
    }

    /// <summary>Показывать ли подпись с именем файла под полем ввода (по умолчанию true).</summary>
    public bool IsFileDisplayVisible
    {
        get => (bool)GetValue(IsFileDisplayVisibleProperty);
        set => SetValue(IsFileDisplayVisibleProperty, value);
    }

    /// <summary>Показывать ли подпись состояния изменения («Изменено») под полем ввода (по умолчанию true).</summary>
    public bool IsModifiedDisplayVisible
    {
        get => (bool)GetValue(IsModifiedDisplayVisibleProperty);
        set => SetValue(IsModifiedDisplayVisibleProperty, value);
    }

    public string LastFileDisplayName
    {
        get => (string)GetValue(LastFileDisplayNameProperty);
        private set => SetValue(LastFileDisplayNameProperty, value);
    }

    public string IsModifiedDisplayText
    {
        get => (string)GetValue(IsModifiedDisplayTextProperty);
        private set => SetValue(IsModifiedDisplayTextProperty, value);
    }

    public Visibility IsModifiedVisibility
    {
        get => (Visibility)GetValue(IsModifiedVisibilityProperty);
        private set => SetValue(IsModifiedVisibilityProperty, value);
    }

    public Visibility EffectiveModifiedVisibility
    {
        get => (Visibility)GetValue(EffectiveModifiedVisibilityProperty);
        private set => SetValue(EffectiveModifiedVisibilityProperty, value);
    }

    public IAsyncRelayCommand LoadFileCommand { get; }
    public IAsyncRelayCommand SaveFileCommand { get; }
    public IRelayCommand CopyCommand { get; }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileContentBoxControl control)
            control.UpdateIsModifiedFromCurrentText();
    }

    private static void OnLastFilePathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileContentBoxControl control)
            control.UpdateLastFileDisplayName();
    }

    private static void OnIsModifiedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileContentBoxControl control)
            control.UpdateIsModifiedDisplay();
    }

    private static void OnButtonsAlignmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileContentBoxControl control)
            control.ApplyButtonsAlignment();
    }

    private static void OnSaveButtonUsesAccentStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileContentBoxControl control)
            control.ApplySaveButtonAccentStyle();
    }

    private static void OnIsModifiedDisplayVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileContentBoxControl control)
            control.UpdateEffectiveModifiedVisibility();
    }

    private void ContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string current = ContentTextBox.Text ?? string.Empty;
        if (current != Text)
            SetValue(TextProperty, current);
        UpdateIsModifiedFromCurrentText();
    }

    private void UpdateIsModifiedFromCurrentText()
    {
        string current = Text ?? string.Empty;
        bool modified = !string.Equals(current, _baselineText, StringComparison.Ordinal);
        if (IsModified != modified)
            IsModified = modified;
    }

    private void UpdateLastFileDisplayName()
    {
        string path = LastFilePath ?? string.Empty;
        LastFileDisplayName = string.IsNullOrEmpty(path)
            ? "Файл не выбран"
            : "Файл: " + path;
    }

    private void UpdateIsModifiedDisplay()
    {
        IsModifiedDisplayText = IsModified ? "Изменено" : string.Empty;
        IsModifiedVisibility = IsModified ? Visibility.Visible : Visibility.Collapsed;
        UpdateEffectiveModifiedVisibility();
    }

    private void UpdateEffectiveModifiedVisibility()
    {
        EffectiveModifiedVisibility = IsModifiedDisplayVisible && IsModified
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private IFilePickerService? GetFilePicker() => App.Services.GetService<IFilePickerService>();

    private void ApplyLoadedContent(string path, string content)
    {
        Text = content;
        _baselineText = content;
        LastFilePath = path;
        IsModified = false;
    }

    private void ApplySavedContent(string path)
    {
        _baselineText = Text ?? string.Empty;
        LastFilePath = path;
        IsModified = false;
    }

    private async Task LoadFileAsync()
    {
        var picker = GetFilePicker();
        if (picker == null)
            return;

        var path = await picker.PickOpenFileAsync(OpenFileTypes, DefaultOpenCommitText);
        if (path == null)
            return;

        var content = await File.ReadAllTextAsync(path);
        ApplyLoadedContent(path, content);
    }

    private async Task SaveFileAsync()
    {
        var picker = GetFilePicker();
        if (picker == null)
            return;

        var path = await picker.PickSaveFileAsync("file", SaveFileTypes, ".txt", DefaultSaveCommitText);
        if (path == null)
            return;

        await File.WriteAllTextAsync(path, Text ?? string.Empty);
        ApplySavedContent(path);
    }

    private void CopyToClipboard()
    {
        var package = new DataPackage();
        package.SetText(Text ?? string.Empty);
        Clipboard.SetContent(package);
    }

    private void ApplyButtonsAlignment()
    {
        if (ButtonsPanel == null)
            return;

        ButtonsPanel.HorizontalAlignment = ButtonsAlignment == ButtonsAlignment.Left
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Right;

        var order = ButtonsAlignment == ButtonsAlignment.Left
            ? new[] { SaveButton, LoadButton, CopyButton }
            : new[] { CopyButton, LoadButton, SaveButton };

        ButtonsPanel.Children.Clear();
        foreach (var button in order)
            ButtonsPanel.Children.Add(button);
    }

    private void ApplySaveButtonAccentStyle()
    {
        if (SaveButton == null)
            return;

        if (!SaveButtonUsesAccentStyle ||
            !Application.Current.Resources.ContainsKey(AccentButtonStyleKey) ||
            Application.Current.Resources[AccentButtonStyleKey] is not Style accentStyle)
        {
            SaveButton.ClearValue(Button.StyleProperty);
            return;
        }

        SaveButton.Style = accentStyle;
    }
}
