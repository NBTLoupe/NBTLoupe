/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Avalonia.Controls;

namespace NBTLoupe.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // The main purpose of this is making sure the user doesn't accidentally lose any edits!
    internal void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // If the user has unsaved changes...
        if (!MainView.MainViewModel.CanSave) return;

        // ...we open a Dialog to warn them and abort the Closing.
        e.Cancel = true;
        MainView.MainViewModel.AbortExitCommand.Execute(null);
    }
}
