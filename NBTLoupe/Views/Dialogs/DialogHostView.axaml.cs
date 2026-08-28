using Avalonia.Controls;
using Avalonia.Interactivity;
using NBTLoupe.ViewModels.Dialogs;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.Views.Dialogs;

public partial class DialogHostView : UserControl
{
    public DialogHostView()
    {
        InitializeComponent();
    }

    // Once an Informational Dialog is loaded...
    internal void InformationalDialog_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;

        switch (mainViewModel.CurrentDialog)
        {
            // ...we focus its corresponding Buttons.
            case AboutDialogViewModel or InfoDialogViewModel or ErrorDialogViewModel:
                DialogOkButton.Focus();
                break;
            case UnsavedChangesDialogViewModel:
                DialogCancelButton.Focus();
                break;
        }
    }
}
