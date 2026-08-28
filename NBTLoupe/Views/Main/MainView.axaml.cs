using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.Views.Main;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        // This is the easiest and most bulletproof way of closing wrong menus, as there's no reason for them to ever be open after a click.
        AddHandler(MenuItem.ClickEvent, OnAnyCommandClicked, RoutingStrategies.Bubble, true);
        AddHandler(Button.ClickEvent, OnAnyCommandClicked, RoutingStrategies.Bubble, true);
    }

    // We need to get the ViewModel for certain operations.
    internal MainViewModel ViewModel => DataContext as MainViewModel ?? throw new InvalidOperationException();

    // This is where the previous AddHandlers hook to.
    private void OnAnyCommandClicked(object? sender, RoutedEventArgs e)
    {
        // Certain MenuItems (like the Recent Files/Folders ones)...
        // and the CommandBar Overflow Menu don't want to close for some reason, so we force them to...
        MainMenuView.CloseIfNeeded();
        MainCommandBarView.CloseIfNeeded();
    }
}
