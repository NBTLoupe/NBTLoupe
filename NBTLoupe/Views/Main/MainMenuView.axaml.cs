using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NBTLoupe.Views.Main;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();

        // Certain MenuItems (like the Recent Files/Folders ones) don't want to close for some reason.
        // This forces the whole MainMenu to close, which should always be the case if something from it was clicked.
        AddHandler(MenuItem.ClickEvent, (_, _) =>
        {
            if (MainMenu.IsOpen) MainMenu.Close();
        }, RoutingStrategies.Bubble, true);
    }
}
