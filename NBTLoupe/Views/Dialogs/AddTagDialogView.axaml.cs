/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NBTLoupe.Views.Dialogs;

public partial class AddTagDialogView : UserControl
{
    public AddTagDialogView()
    {
        InitializeComponent();
    }

    // Once the Dialog's main TextBox is loaded...
    internal void DialogTextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;

        // We Focus it and Select its text.
        textBox?.Focus();
        textBox?.SelectAll();
    }
}
