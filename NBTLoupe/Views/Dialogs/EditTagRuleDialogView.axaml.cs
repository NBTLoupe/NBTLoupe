using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NBTLoupe.Views.Dialogs;

public partial class EditTagRuleDialogView : UserControl
{
    public EditTagRuleDialogView()
    {
        InitializeComponent();
    }

    // Once the Dialog's main TextBox is loaded...
    internal void DialogTextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;

        // We Focus it and Select its text.
        textBox?.Focus();
        textBox?.SelectAll();
    }
}
