/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NBTLoupe.Views.Main;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();

        // Certain MenuItems (like the Recent Files/Folders ones) don't want to close for some reason.
        // This forces the whole MainMenu to close, which should always be the case if something from it was clicked.
        AddHandler(MenuItem.ClickEvent, (_, _) =>
        {
            if (MainMenu.IsOpen) MainMenu.Close();
        }, RoutingStrategies.Bubble, true);
    }
}
