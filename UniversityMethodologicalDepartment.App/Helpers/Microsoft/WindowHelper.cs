// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Helpers.Microsoft;

// Helper class to allow the app to find the Window that contains an
// arbitrary UIElement (GetWindowForElement). To do this, we keep track
// of all active Windows. The app code must call WindowHelper.CreateWindow
// rather than "new Window" so we can keep track of all the relevant
// windows. In the future, we would like to support this in platform APIs.
public partial class WindowHelper
{
    public static Window CreateWindow()
    {
        var newWindow = new Window();
        TrackWindow(newWindow);
        return newWindow;
    }

    public static void TrackWindow(Window window)
    {
        window.Closed += (sender, args) =>
        {
            _activeWindows.Remove(window);
        };
        _activeWindows.Add(window);
    }

    public static Window? GetWindowForElement(UIElement element)
    {
        if (element.XamlRoot != null)
        {
            foreach (Window window in _activeWindows)
            {
                if (element.XamlRoot == window.Content.XamlRoot)
                {
                    return window;
                }
            }
        }
        return null;
    }
    // get dpi for an element
    public static double GetRasterizationScaleForElement(UIElement element)
    {
        if (element.XamlRoot != null)
        {
            foreach (Window window in _activeWindows)
            {
                if (element.XamlRoot == window.Content.XamlRoot)
                {
                    return element.XamlRoot.RasterizationScale;
                }
            }
        }
        return 0.0;
    }

    public static void SetWindowMinSize(Window window, double width, double height)
    {
        if (window.Content is not FrameworkElement windowContent)
        {
            System.Diagnostics.Debug.WriteLine("Window content is not a FrameworkElement.");
            return;
        }

        if (windowContent.XamlRoot is null)
        {
            System.Diagnostics.Debug.WriteLine("Window content's XamlRoot is null.");
            return;
        }

        if (window.AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            System.Diagnostics.Debug.WriteLine("Window's AppWindow.Presenter is not an OverlappedPresenter.");
            return;
        }

        var scale = windowContent.XamlRoot.RasterizationScale;
        var minWidth = width * scale;
        var minHeight = height * scale;
        presenter.PreferredMinimumWidth = (int)minWidth;
        presenter.PreferredMinimumHeight = (int)minHeight;
    }

    public static List<Window> ActiveWindows => _activeWindows;

    private static readonly List<Window> _activeWindows = [];

    public static StorageFolder GetAppLocalFolder()
    {
        StorageFolder localFolder;
        if (!NativeMethods.IsAppPackaged)
        {
            localFolder = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(System.AppContext.BaseDirectory)).Result;
        }
        else
        {
            localFolder = ApplicationData.Current.LocalFolder;
        }
        return localFolder;
    }
}
