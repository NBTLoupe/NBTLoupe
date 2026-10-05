/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Avalonia.Controls;
using Avalonia.Interactivity;
using NBTLoupe.ViewModels.Dialogs;

namespace NBTLoupe.Views.Dialogs;

public partial class EditTagDialogView : UserControl
{
    public EditTagDialogView()
    {
        InitializeComponent();
    }

    // Once a Dialog's main TextBox is loaded...
    internal void DialogTextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EditTagDialogViewModel editTagDialogViewModel) return;
        var textBox = sender as TextBox;

        // If we're on a Rename Dialog, and the Loaded TextBox is the Value one, we ignore it.
        if (editTagDialogViewModel.IsRename && textBox?.Name == "EditValueTextBox") return;

        // If not, we Focus it and Select its text.
        textBox?.Focus();
        textBox?.SelectAll();
    }
}
