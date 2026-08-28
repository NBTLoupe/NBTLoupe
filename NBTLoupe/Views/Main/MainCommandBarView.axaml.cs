using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace NBTLoupe.Views.Main;

public partial class MainCommandBarView : UserControl
{
    public MainCommandBarView()
    {
        InitializeComponent();

        // The CommandBar Overflow Menu doesn't want to close for some reason.
        // This forces it to close, but only when the button is the Child of an "PART_OverflowPresenter".
        // We check this so the Overflow Menu doesn't close unexpectedly, and instead only does when one of its items was clicked (which should always be the case).
        AddHandler(Button.ClickEvent, (_, e) =>
        {
            if ((e.Source as Control).FindAncestorOfType<ItemsControl>()?.Name == "PART_OverflowPresenter" &&
                MainCommandBar.IsOpen)
                MainCommandBar.IsOpen = false;
        }, RoutingStrategies.Bubble, true);
    }
}
