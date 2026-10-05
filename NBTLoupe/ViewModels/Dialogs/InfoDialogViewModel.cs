/*
 *  Copyright (c) 2024-2026 MallardLuna
 *  
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Threading.Tasks;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the Info Dialog!
internal class InfoDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal InfoDialogViewModel(MainViewModel mainViewModel, string message) : base(mainViewModel)
    {
        // And we set the MessageText!
        MessageText = message;
    }

    // Here's all the fields we bind to in the XAML...
    // The Message TextBlock
    internal string MessageText { get; }

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }
}
