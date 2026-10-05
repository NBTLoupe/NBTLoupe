/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using Avalonia.Controls;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.Views.Main;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    // We need to get the ViewModel for certain operations.
    internal MainViewModel MainViewModel => DataContext as MainViewModel ?? throw new InvalidOperationException();
}
