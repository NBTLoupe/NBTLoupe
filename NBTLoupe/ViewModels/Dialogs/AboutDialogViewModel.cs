/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the About Dialog!
internal partial class AboutDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal AboutDialogViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
    }

    // We set the AboutDialog's title from here as it's neater to have the current version in there.
    internal static string AboutTitle => $"About {Program.FullName}";

    // This allows us to have special buttons to link to our Website and our GitHub!
    internal override IReadOnlyList<DialogButton> SpecialButtons =>
        [new("Website", WebsiteCommand), new("GitHub", GitHubCommand)];

    // This allows us to hide the Dialog Cancel Button, as it is redundant here!
    internal override bool IsCancelNeeded => false;

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }

    // This one is executed when the user chooses to visit our Website.
    [RelayCommand]
    private Task<bool> Website()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            await MainViewModel.TopLevel.Launcher.LaunchUriAsync(new Uri("https://nbtloupe.mallardluna.com"));
        });
    }

    // This one is executed when the user chooses to view our source code on GitHub.
    [RelayCommand]
    private Task<bool> GitHub()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            await MainViewModel.TopLevel.Launcher.LaunchUriAsync(new Uri("https://github.com/NBTLoupe/NBTLoupe"));
        });
    }
}
