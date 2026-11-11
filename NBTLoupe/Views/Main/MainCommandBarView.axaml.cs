/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace NBTLoupe.Views.Main;

public partial class MainCommandBarView : UserControl
{
    public MainCommandBarView()
    {
        InitializeComponent();

        // The CommandBar Overflow Menu doesn't want to close for some reason.
        // This forces it to close, but only when the button is the Child of an "PART_OverflowPresenter".
        // We check this so the Overflow Menu doesn't close unexpectedly, and instead only does when one of its items was clicked (which should always be the case).
        AddHandler(Button.ClickEvent, (_, e) =>
        {
            if ((e.Source as Control)?.FindAncestorOfType<ItemsControl>()?.Name == "PART_OverflowPresenter" &&
                MainCommandBar.IsOpen)
                MainCommandBar.IsOpen = false;
        }, RoutingStrategies.Bubble, true);
    }
}
