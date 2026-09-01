using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using NBTLoupe.ViewModels.Dialogs;

namespace NBTLoupe.Views.Dialogs;

public partial class FindBasicDialogView : UserControl
{
    public FindBasicDialogView()
    {
        InitializeComponent();
    }

    // Because these "Tabs" are fake and each Find mode is a completely different Dialog, we need to intercept this event and redirect to the ViewModel to do the switch.
    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // We do have to double-check the tab that was "selected" is the opposite of this Dialog, though, or it'll break into an infinite switching loop.
        if (e.AddedItems.OfType<TabStripItem>().Any(x => x.Name == "Advanced") &&
            DataContext is FindBasicDialogViewModel findBasicDialogViewModel)
            findBasicDialogViewModel.DialogSwitchModesCommand.Execute(null);
    }
}
