using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NBTModel.Data.Nodes;
using Serilog;
using Substrate.Nbt;

namespace NBTExplorer;

public partial class MainWindow
{
    // We need a way to disable the Clipboard-based features if they wouldn't work.
    private bool ClipboardAvailable => Clipboard is not null;

    // This is how when lazily load items when the user expands them UI-wise.
    internal async void TreeViewItem_OnExpanded(object? sender, RoutedEventArgs e)
    {
        try
        {
            // We get the TreeViewItem that was expanded, and its related TreeNode.
            if (e.Source is not TreeViewItem { DataContext: TreeNode treeNode }) return;

            // Check if SubNodes is null.
            if (treeNode.SubNodes is null) throw new UnreachableException();

            // We Expand its real children lazily, and Stage them...
            var staged = new ObservableCollection<TreeNode>();
            await WithBlock(
                async () => await TreeNode.ExpandNodeAsync(treeNode.DataNode.Nodes, staged, treeNode), true);

            // ...so we can replace our stubby/lazy SubNodes with the Staged ones.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                treeNode.SubNodes.Clear();
                foreach (var node in staged) treeNode.SubNodes.Add(node);
            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Error(ex, "[neoNBTExplorer]: Unhandled UI thread exception");
            OpenDialog(new ErrorDialogState(ex));
        }
    }

    // Basically all this does is toggle the Expand/Collapse functionality if you right-click a Tag.
    internal void ContextMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        // Well, except if you selected more than one. Then we just don't show the Context Menu at all.
        if (SelectedTreeNodes.Count != 1)
        {
            e.Cancel = true;
            return;
        }

        var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();

        // Immediately return if selectedTreeNode is null.
        if (selectedTreeNode is null) throw new UnreachableException();

        // Show the corresponding Button, depending on if the SelectedTreeNode is already IsExpanded (UI-wise) or not. 
        Expand.IsVisible = !selectedTreeNode.IsExpanded;
        Collapse.IsVisible = selectedTreeNode.IsExpanded;

        // Only show the Expand Children/Expand Tree Buttons if it IsExpanded (UI-wise)
        ExpandChildren.Toggle(selectedTreeNode.IsExpanded);
        ExpandTree.Toggle(selectedTreeNode.IsExpanded);
    }

    // When the TreeNode selection changes, certain actions are allowed and others not. This function checks them all.
    // These are extremely self-explanatory, so I'm not going to comment them.
    private async Task SelectionChanged()
    {
        var single = SelectedTreeNodes.Count == 1 ? SelectedTreeNodes[0] : null;

        ToggleExpand.Toggle(single?.SubNodes?.Count > 0);

        OpenInExplorer.Toggle(single?.DataNode is DirectoryDataNode);

        MoveUp.Toggle(single?.DataNode is { CanReoderNode: true, CanMoveNodeUp: true });
        MoveDown.Toggle(single?.DataNode is { CanReoderNode: true, CanMoveNodeDown: true });

        Refresh.Toggle(single?.DataNode?.CanRefreshNode ?? false);

        Cut.Toggle(ClipboardAvailable && single?.DataNode?.CanCutNode == true);
        Copy.Toggle(ClipboardAvailable && single?.DataNode?.CanCopyNode == true);
        Paste.Toggle(ClipboardAvailable && single?.DataNode is not null && await single.DataNode.CanPasteIntoNode());

        Rename.Toggle(single?.DataNode?.CanRenameNode ?? false);
        EditValue.Toggle(single?.DataNode?.CanEditNode ?? false);
        Delete.Toggle(SelectedTreeNodes.Count > 0 && SelectedTreeNodes.All(x => x.DataNode.CanDeleteNode));

        AddByteTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_BYTE) ?? false);
        AddShortTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_SHORT) ?? false);
        AddIntTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_INT) ?? false);
        AddLongTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_LONG) ?? false);
        AddFloatTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_FLOAT) ?? false);
        AddDoubleTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_DOUBLE) ?? false);
        AddByteArrayTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_BYTE_ARRAY) ?? false);
        AddIntArrayTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_INT_ARRAY) ?? false);
        AddLongArrayTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_LONG_ARRAY) ?? false);
        AddStringTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_STRING) ?? false);
        AddListTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_LIST) ?? false);
        AddCompoundTag.Toggle(single?.DataNode?.CanCreateTag(TagType.TAG_COMPOUND) ?? false);

        Find.Toggle(single?.DataNode?.CanSearchNode ?? false);
    }

    // This opens the EditDialog when the user double-clicks a supported item.
    internal void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // We check if the user is double-clicking a true item.
        var treeViewItem = (e.Source as Control)?.FindAncestorOfType<TreeViewItem>(true);
        if (treeViewItem is null) return;

        if (EditValue.CanExecute(null)) EditValue.Execute(null);
        else if (Rename.CanExecute(null)) Rename.Execute(null);
    }

    // Once an Informational Dialog is loaded...
    internal void InformationalDialog_OnLoaded(object? sender, RoutedEventArgs e)
    {
        // ...we focus its corresponding Buttons.
        if (CurrentDialog is AboutDialogState or ErrorDialogState) DialogOkButton.Focus();
        if (CurrentDialog is UnsavedChangesDialogState) DialogCancelButton.Focus();
    }

    // Once a Dialog's main TextBox is loaded...
    internal void DialogTextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;

        // If we're on a Rename Dialog, and the Loaded TextBox is the Value one, we ignore it.
        if (CurrentDialog is EditTagDialogState { IsRename: true } && textBox?.Name == "EditValueTextBox") return;

        // If not, we Focus it and Select its text.
        textBox?.Focus();
        textBox?.SelectAll();
    }

    // The main purpose of this is making sure the user doesn't accidentally lose any edits!
    internal void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // If the user has unsaved changes...
        if (!Save.CanExecute(null)) return;

        // ...we open a Dialog to warn them and abort the Closing.
        OpenDialog(new UnsavedChangesDialogState(this));
        e.Cancel = true;
    }

    // ReSharper disable UnusedMember.Global
    // Our Drag support... (AKA the effect that tells the user they can drag a file into the app)
    internal void TreeView_OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Items.Count == 1 && e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    // ...and our Drop support! (AKA actually processing what they dropped into the app)
    internal async void TreeView_OnDrop(object? sender, DragEventArgs e)
    {
        try
        {
            var item = e.DataTransfer.TryGetFiles();
            if (item?.Length != 1) return;

            switch (item[0])
            {
                case null:
                    return;
                case IStorageFile file:
                    await OpenFileAsync(file.Path.LocalPath);
                    break;
                case IStorageFolder folder:
                    await OpenFolderAsync(folder.Path.LocalPath);
                    break;
            }
        }
        catch (Exception ex)
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Error(ex, "[neoNBTExplorer]: Unhandled UI thread exception");
            OpenDialog(new ErrorDialogState(ex));
        }
    }
    // ReSharper restore UnusedMember.Global
}