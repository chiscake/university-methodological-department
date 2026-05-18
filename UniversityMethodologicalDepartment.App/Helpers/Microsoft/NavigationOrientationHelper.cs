// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

using UniversityMethodologicalDepartment.App;

namespace Helpers.Microsoft;

public static partial class NavigationOrientationHelper
{
    public static bool IsLeftMode()
    {
        return SettingsHelper.Current.IsLeftMode;
    }

    public static void IsLeftModeForElement(bool isLeftMode)
    {
        // Пока отключено: вызов вызывал сбой в Microsoft.ui.xaml.dll (0xC0000005).
        // UpdateNavigationViewForElement(isLeftMode);
        SettingsHelper.Current.IsLeftMode = isLeftMode;
    }

    public static void UpdateNavigationViewForElement(bool isLeftMode)
    {
        var navView = App.MainWindow?.NavigationView;
        if (navView == null)
            return;

        var mode = isLeftMode ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
        var queue = navView.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        // Низкий приоритет: установка после отрисовки, чтобы снизить риск сбоя в нативном коде.
        queue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            try
            {
                if (App.MainWindow?.NavigationView is NavigationView nv)
                    nv.PaneDisplayMode = mode;
            }
            catch
            {
                // Игнорируем ошибки при установке PaneDisplayMode (возможен сбой в WinUI).
            }
        });
    }
}
