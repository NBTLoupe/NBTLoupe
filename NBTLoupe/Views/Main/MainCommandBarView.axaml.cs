using Avalonia.Controls;

namespace NBTLoupe.Views.Main;

public partial class MainCommandBarView : UserControl
{
    public MainCommandBarView()
    {
        InitializeComponent();
    }

    // We need a way to Close the MainCommandBar from its parent.
    internal void CloseIfNeeded()
    {
        if (MainCommandBar.IsOpen) MainCommandBar.IsOpen = false;
    }
}
