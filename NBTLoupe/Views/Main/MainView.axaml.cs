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
