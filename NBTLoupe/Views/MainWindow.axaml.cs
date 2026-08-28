using Avalonia.Controls;

namespace NBTLoupe.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // The main purpose of this is making sure the user doesn't accidentally lose any edits!
    internal void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // If the user has unsaved changes...
        if (!MainView.ViewModel.CanSave) return;

        // ...we open a Dialog to warn them and abort the Closing.
        e.Cancel = true;
        MainView.ViewModel.AbortExitCommand.Execute(null);
    }
}
