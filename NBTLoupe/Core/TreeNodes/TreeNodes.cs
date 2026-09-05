using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.ViewModels.Dialogs;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data.Nodes;
using Serilog;

namespace NBTLoupe.Core.TreeNodes;

// TreeNode implementation to be able to interface with Avalonia's TreeView.
internal partial class TreeNode : ObservableObject
{
    // This is related to the NodeTreeComparer, defined in another file.
    private static readonly NodeTreeComparer NodeComparer = new();

    // We need this exclusively for the lazy loading's Error Handling.
    private readonly Func<Func<MainViewModel, DialogHostViewModel>, Task<bool>> _openDialogAsync;
    private readonly Func<Func<Task>, bool, bool, Task> _withBlock;

    // And here's how you create the actual TreeNode!
    private TreeNode(DataNode dataNode, ObservableCollection<TreeNode> subNodes,
        Func<Func<Task>, bool, bool, Task> withBlock,
        Func<Func<MainViewModel, DialogHostViewModel>, Task<bool>> openDialogAsync, bool isPlaceholder = false)
    {
        _withBlock = withBlock;
        _openDialogAsync = openDialogAsync;

        DataNode = dataNode;
        SubNodes = subNodes;
        IsPlaceholder = isPlaceholder;
        Title = dataNode.NodeDisplay;
    }

    // ...it includes its children (SubNodes), its data (DataNode), and its Parent.
    internal ObservableCollection<TreeNode>? SubNodes { get; }
    internal DataNode DataNode { get; }
    internal TreeNode? Parent { get; private set; }
    internal bool IsPlaceholder { get; }

    // Oh, but all that data is for our fun. Avalonia cares about its Title and its Icon, which is here.
    [ObservableProperty] internal partial string Title { get; private set; }

    internal string Icon => DataNode switch
    {
        // Here's the list that matches a FluentIcon to each DataNode Type!
        TagByteDataNode => "NumberCircle1",
        TagShortDataNode => "NumberCircle2",
        TagIntDataNode => "NumberCircle4",
        TagLongDataNode => "NumberCircle8",
        TagFloatDataNode => "DecimalArrowLeft",
        TagDoubleDataNode => "DecimalArrowRight",

        TagByteArrayDataNode => "CodeBlock",
        TagIntArrayDataNode => "DataBarVertical",
        TagLongArrayDataNode => "DataBarHorizontal",

        TagStringDataNode => "TextT",
        TagListDataNode => "TextBulletList",

        TagCompoundDataNode => "Box",

        DirectoryDataNode => "Folder",
        NbtFileDataNode => "Archive",
        RegionChunkDataNode => "Archive",

        RegionFileDataNode => "Cube",
        CubicRegionDataNode => "Cube",

        _ => "QuestionCircle"
    };

    // Create an IsExpanded property.
    [ObservableProperty] internal partial bool IsExpanded { get; set; }

    // This is how we lazily load items when the user expands them UI-wise.
    async partial void OnIsExpandedChanged(bool value)
    {
        try
        {
            if (!value) return;

            // Check if SubNodes is null.
            if (SubNodes is null) throw new UnreachableException();

            // We lazy-load its children.
            await _withBlock(() => LazyLoadAsync(), false, false);
        }
        catch (Exception ex)
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Error(ex, "[NBTLoupe]: Unhandled UI thread exception");
            await _openDialogAsync(viewModel => new ErrorDialogViewModel(viewModel, ex));
        }
    }

    // Oh, and this is how you refresh its Title if you have to.
    internal void RefreshTitle()
    {
        Title = DataNode.NodeDisplay;
    }

    // Oh, and here's how you set its parent!
    private void SetParent(TreeNode? parent)
    {
        Parent = parent;
    }
}
