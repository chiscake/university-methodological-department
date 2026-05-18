using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UniversityMethodologicalDepartment.App.Controls;

public sealed partial class SemesterSelectorControl : UserControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(SemesterSelectorControl),
            new PropertyMetadata("Семестр:", OnHeaderChanged));

    public static readonly DependencyProperty SelectedSemesterProperty =
        DependencyProperty.Register(
            nameof(SelectedSemester),
            typeof(int),
            typeof(SemesterSelectorControl),
            new PropertyMetadata(1, OnSelectedSemesterChanged));

    private bool _isSyncingSelection;

    public SemesterSelectorControl()
    {
        InitializeComponent();
        UpdateHeaderVisibility();
        UpdateSelectionFromValue(SelectedSemester);
    }

    public event EventHandler<int>? SemesterChanged;

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public int SelectedSemester
    {
        get => (int)GetValue(SelectedSemesterProperty);
        set => SetValue(SelectedSemesterProperty, value);
    }

    private static void OnSelectedSemesterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SemesterSelectorControl control || e.NewValue is not int semester)
        {
            return;
        }

        control.UpdateSelectionFromValue(semester);
    }

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SemesterSelectorControl control)
        {
            control.UpdateHeaderVisibility();
        }
    }

    private void Selector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_isSyncingSelection)
        {
            return;
        }

        if (sender.SelectedItem is not SelectorBarItem selectedItem ||
            selectedItem.Tag is not string tag ||
            !int.TryParse(tag, out var semester))
        {
            Debug.WriteLine("[SemesterSelectorControl] Invalid semester tag value.");
            return;
        }

        if (semester == SelectedSemester)
        {
            return;
        }

        SelectedSemester = semester;
        SemesterChanged?.Invoke(this, semester);
    }

    private void UpdateSelectionFromValue(int semester)
    {
        _isSyncingSelection = true;
        try
        {
            foreach (var item in Selector.Items.OfType<SelectorBarItem>())
            {
                if (item.Tag is string tag && int.TryParse(tag, out var itemSemester) && itemSemester == semester)
                {
                    Selector.SelectedItem = item;
                    return;
                }
            }
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void UpdateHeaderVisibility()
    {
        HeaderTextBlock.Visibility = string.IsNullOrWhiteSpace(Header)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
