using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniversityMethodologicalDepartment.App.Contracts;

namespace UniversityMethodologicalDepartment.App.Pages.Shared;

public sealed partial class DataStatusBar : UserControl
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(
            nameof(Status),
            typeof(DataStatus),
            typeof(DataStatusBar),
            new PropertyMetadata(DataStatus.Loading, OnStatusChanged));

    public DataStatusBar()
    {
        this.InitializeComponent();
        ApplyStatus(Status);
    }

    public DataStatus Status
    {
        get => (DataStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataStatusBar control && e.NewValue is DataStatus status)
            control.ApplyStatus(status);
    }

    private void ApplyStatus(DataStatus status)
    {
        var styleKey = status switch
        {
            DataStatus.Cached => "AttentionIconInfoBadgeStyle",
            DataStatus.Loading => "InformationalIconInfoBadgeStyle",
            _ => "SuccessIconInfoBadgeStyle"
        };

        if (Application.Current.Resources.TryGetValue(styleKey, out var style) && style is Style badgeStyle)
        {
            StatusBadge.Style = badgeStyle;
        }
        else
        {
            StatusBadge.ClearValue(StyleProperty);
        }

        StatusText.Text = GetDisplayName(status);
    }

    private static string GetDisplayName(DataStatus status)
    {
        var member = typeof(DataStatus).GetMember(status.ToString());
        if (member.Length > 0)
        {
            var display = member[0].GetCustomAttribute<DisplayAttribute>();
            if (!string.IsNullOrWhiteSpace(display?.Name))
                return display.Name!;
        }

        return status.ToString();
    }
}
