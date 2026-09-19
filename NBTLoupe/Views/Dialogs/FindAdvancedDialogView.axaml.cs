using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using NBTLoupe.Core.TreeNodes;
using NBTLoupe.ViewModels.Dialogs;

namespace NBTLoupe.Views.Dialogs;

public partial class FindAdvancedDialogView : UserControl
{
    public FindAdvancedDialogView()
    {
        InitializeComponent();
    }

    // This opens the corresponding Nested Dialog when the user double-clicks a supported item.
    internal void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // We check if the user is double-clicking a true item.
        var ancestor = (e.Source as Control)?.FindAncestorOfType<TreeViewItem>(true);
        if (ancestor is null) return;

        if (DataContext is not FindAdvancedDialogViewModel findAdvancedDialogViewModel) return;

        // We check the DataContext of the Ancestor, as the DoubleTappedItem RelayCommand needs to know if it is a TreeNode or not.
        switch (ancestor.DataContext)
        {
            case TreeNode:
                findAdvancedDialogViewModel.DoubleTappedItemCommand.Execute(true);
                break;
            case TreeRule:
                findAdvancedDialogViewModel.DoubleTappedItemCommand.Execute(false);
                break;
        }
    }

    // Because these "Tabs" are fake and each Find mode is a completely different Dialog, we need to intercept this event and redirect to the ViewModel to do the switch.
    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // We do have to double-check the tab that was "selected" is the opposite of this Dialog, though, or it'll break into an infinite switching loop.
        if (e.AddedItems.OfType<TabStripItem>().Any(x => x.Name == "Basic") &&
            DataContext is FindAdvancedDialogViewModel findAdvancedDialogViewModel)
            findAdvancedDialogViewModel.DialogSwitchModesCommand.Execute(null);
    }
}
