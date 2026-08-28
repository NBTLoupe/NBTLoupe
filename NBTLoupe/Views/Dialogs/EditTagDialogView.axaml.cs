using Avalonia.Controls;
using Avalonia.Interactivity;
using NBTLoupe.ViewModels.Dialogs;

namespace NBTLoupe.Views.Dialogs;

public partial class EditTagDialogView : UserControl
{
    public EditTagDialogView()
    {
        InitializeComponent();
    }

    // Once a Dialog's main TextBox is loaded...
    internal void DialogTextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EditTagDialogViewModel editTagDialogViewModel) return;
        var textBox = sender as TextBox;

        // If we're on a Rename Dialog, and the Loaded TextBox is the Value one, we ignore it.
        if (editTagDialogViewModel.IsRename && textBox?.Name == "EditValueTextBox") return;

        // If not, we Focus it and Select its text.
        textBox?.Focus();
        textBox?.SelectAll();
    }
}
