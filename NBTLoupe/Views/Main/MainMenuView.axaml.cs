using Avalonia.Controls;

namespace NBTLoupe.Views.Main;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();
    }

    // We need a way to Close the MainMenu from its parent.
    internal void CloseIfNeeded()
    {
        if (MainMenu.IsOpen) MainMenu.Close();
    }
}
