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
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.Views.Dialogs;

public partial class DialogHostView : UserControl
{
    public DialogHostView()
    {
        InitializeComponent();
    }

    // Once any Dialog is loaded...
    internal void Dialog_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;

        // ...we focus it. (So the KeyBinds work.)
        DialogPanel.Focus();

        // And if it is informational...
        switch (mainViewModel.CurrentDialog)
        {
            // ...we focus its corresponding Buttons.
            case AboutDialogViewModel or InfoDialogViewModel or ErrorDialogViewModel:
                DialogOkButton.Focus();
                break;
            case UnsavedChangesDialogViewModel:
                DialogCancelButton.Focus();
                break;
        }
    }
}
