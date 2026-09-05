using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using NBTLoupe.ViewModels.Dialogs;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data.Nodes;

namespace NBTLoupe.Core.TreeNodes;

// This sets up the TreeNode to be able to be displayed in the UI.
internal partial class TreeNode
{
    // Nodes in NBTModel have to be "Expanded" to be able to access their children. This does that in a sorted manner.
    internal static void ExpandNode(IList<DataNode> nodeTree, ObservableCollection<TreeNode> treeNodes,
        Func<Func<Task>, bool, bool, Task> withBlock,
        Func<Func<MainViewModel, DialogHostViewModel>, Task<bool>> openDialogAsync,
        TreeNode? parent = null)
    {
        // First we sort the NodeTree...
        var sortedNodeTree = nodeTree.OrderBy(dataNode => dataNode, NodeComparer);

        foreach (var dataNode in sortedNodeTree)
        {
            // ...then call the previously mentioned Expand method in each of its children.
            dataNode.Expand();

            // Once that's done, we can create its respective SubNodes collection...
            var subNodes = new ObservableCollection<TreeNode>();

            // ...and initialize a new TreeNode with it.
            var treeNode = new TreeNode(dataNode, subNodes, withBlock, openDialogAsync);
            treeNode.SetParent(parent);

            // And finally, we can add the Expanded one back to its parent.
            treeNodes.Add(treeNode);

            // We do need to add a Placeholder so the arrow shows, though!
            if (dataNode.Nodes.Count < 1) continue;

            var placeholder = new TreeNode(new TagStringDataNode(""), [], withBlock, openDialogAsync, true);
            placeholder.SetParent(parent);
            subNodes.Add(placeholder);
        }
    }

    // This function helps with lazy-loading, mainly ensuring the Placeholder gets deleted.
    internal async Task LazyLoadAsync(IList<DataNode>? dataNode = null)
    {
        if (SubNodes is not [{ IsPlaceholder: true }]) return;

        // We Expand its real children lazily, and Stage them...
        var staged = new ObservableCollection<TreeNode>();
        ExpandNode(dataNode ?? DataNode.Nodes, staged, _withBlock, _openDialogAsync, this);

        // ...so we can replace our stubby/lazy SubNodes with the Staged ones.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SubNodes.Clear();
            foreach (var node in staged) SubNodes.Add(node);
        }, DispatcherPriority.Background);
    }

    // This IsExpands (UI-wise) an entire TreeNode. Related to the ExpandTree RelayCommand. 
    internal async Task ExpandTreeAsync()
    {
        switch (SubNodes)
        {
            // We immediately return if it doesn't have SubNodes...
            case null:
                return;
            // ...then we lazy-load its children...
            case [{ IsPlaceholder: true }]:
                await LazyLoadAsync();
                break;
        }

        // ... then we expand the TreeNode itself...
        await Dispatcher.UIThread.InvokeAsync(() => IsExpanded = true, DispatcherPriority.Background);

        // ...and we loop until the entire TreeNode IsExpanded (UI-wise).
        foreach (var child in SubNodes.ToList()) await child.ExpandTreeAsync();
    }

    // This IsExpands (UI-wise) an entire TreeNode, but the other way around. 
    internal async Task ExpandTreeReverseAsync()
    {
        // If it has a Parent, we loop until the entire TreeNode IsExpanded (UI-wise).
        if (Parent is not null) await Parent.ExpandTreeReverseAsync();

        // And this is how we expand the TreeNode itself.
        await Dispatcher.UIThread.InvokeAsync(() => IsExpanded = true, DispatcherPriority.Background);
    }

    // This refreshes a TreeNode. Required to display in the UI any change in it.
    internal async Task RefreshChildNodesAsync()
    {
        // Immediately return if it doesn't have SubNodes.
        if (SubNodes is null) return;

        // First we back up the current SubNodes...
        var currentNodes = SubNodes.ToDictionary(treeNode => treeNode.DataNode, treeNode => treeNode);

        // Then we sort the NodeTree...
        var sortedNodeTree = DataNode.Nodes.OrderBy(dataNode => dataNode, NodeComparer);

        // And we stage our SubNodes...
        var staged = new ObservableCollection<TreeNode>();

        foreach (var child in sortedNodeTree)
            // Then for each already-Expanded child (from the currentNodes)...
            if (currentNodes.TryGetValue(child, out var existing))
            {
                // ...we readd it to the SubNodes, and Refresh it if needed.
                existing.SetParent(this);
                if (!child.HasUnexpandedChildren) await existing.RefreshChildNodesAsync();
                staged.Add(existing);
            }
            // ...and if the child isn't expanded...
            else if (!child.IsExpanded)
            {
                // ...we expand it...
                child.Expand();

                // ...then create a TreeNode from scratch for it....
                var newSubNodes = new ObservableCollection<TreeNode>();
                var newTreeNode = new TreeNode(child, newSubNodes, _withBlock, _openDialogAsync);
                newTreeNode.SetParent(this);

                // ...and add it to the SubNodes.
                staged.Add(newTreeNode);
            }

        if (DataNode.Nodes.Count > 0)
        {
            // If we didn't add any children, we prepare the parent for lazy-loading.
            var placeholder = new TreeNode(new TagStringDataNode(""), [], _withBlock, _openDialogAsync, true);
            placeholder.SetParent(this);
            staged.Add(placeholder);
        }

        // ...and add the staged ones all at once.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SubNodes.Clear();
            foreach (var node in staged) SubNodes.Add(node);
        }, DispatcherPriority.Background);

        // Then we refresh its Title. Usually not necessary, but useful when the root is being Refreshed.
        RefreshTitle();
    }
}
