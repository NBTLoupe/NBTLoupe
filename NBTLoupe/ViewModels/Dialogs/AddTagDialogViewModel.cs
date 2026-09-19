using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core.TreeNodes;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the AddTag Dialog!
internal partial class AddTagDialogViewModel : DialogHostViewModel
{
    // We save the Dialog TagType here to be able to use it later on.
    private readonly TagType _dialogTagType;

    // Here we set up the Dialog!
    internal AddTagDialogViewModel(MainViewModel mainViewModel, TagType? tagType = null,
        FindAdvancedDialogViewModel? parent = null) : base(mainViewModel, parent)
    {
        _dialogTagType = (tagType ?? parent?.NewTagType) ?? throw new InvalidOperationException();

        // Set the context-accurate Title and Type.
        TitleText = $"Add {_dialogTagType.GetFriendlyTag()}";
    }

    // This is so we grab our staging TreeNode when this is opened as the Find and Replace's Nested Dialog. 
    private TreeNode? SingleSelectedTreeNode => Parent is FindAdvancedDialogViewModel parent
        ? parent.SingleSelectedReplacementTag
        : MainViewModel.SingleSelectedTreeNode;

    // This is so we auto-select on our staging TreeNode when this is opened as the Find and Replace's Nested Dialog. 
    private ObservableCollection<TreeNode> SelectedTreeNodes => Parent is FindAdvancedDialogViewModel parent
        ? parent.SelectedReplacementTags
        : MainViewModel.SelectedTreeNodes;

    // Here's all the fields we bind to in the XAML...
    // The Title TextBlock...
    internal string TitleText { get; }

    // The Tag Name TextBox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? TagName { get; set; }

    // The Tag Size NumericUpDown...
    [ObservableProperty] public partial decimal TagSize { get; set; }

    // ...(which is only enabled in certain cases, by the way)
    internal bool SizeEnabled => _dialogTagType is TagType.TAG_BYTE_ARRAY or TagType.TAG_SHORT_ARRAY
        or TagType.TAG_INT_ARRAY or TagType.TAG_LONG_ARRAY;

    // And here's where our Validation magic happens!
    internal override bool IsOkEnabled
    {
        get
        {
            // Only enable the OK button if:
            // - The user inputted a Name.
            // - There isn't already a sibling with that same Name.
            if (string.IsNullOrEmpty(TagName)) return false;
            var metaTagContainer = SingleSelectedTreeNode?.DataNode as IMetaTagContainer;
            return metaTagContainer?.NamedTagContainer is null ||
                   !metaTagContainer.NamedTagContainer.TagNamesInUse.Contains(TagName);
        }
    }

    // This gives the OK button tailor-made text!
    internal override string OkText => "Add";

    partial void OnTagSizeChanging(decimal value)
    {
        if (value < 0) TagSize = 0;
    }

    // And here's the actual magic! The OK button!
    internal sealed override async Task ExecuteAsync()
    {
        // Check if SubNodes is null, and return if so.
        if (SingleSelectedTreeNode?.SubNodes is null) throw new UnreachableException();

        // Save its parent's SubNodes.
        var before = SingleSelectedTreeNode.SubNodes.Select(n => n.DataNode).ToHashSet();

        // Create the new TreeNode.
        if (!SingleSelectedTreeNode.DataNode.CreateNode(_dialogTagType, TagName ?? "", (int)TagSize))
            throw new UnreachableException();

        // IsExpand (UI-wise) the new TreeNode.
        SingleSelectedTreeNode.IsExpanded = true;

        // Refresh its parent.
        await SingleSelectedTreeNode.RefreshChildNodesAsync();

        // And find the new TreeNode, so we can Select it.
        var newFound =
            SingleSelectedTreeNode.SubNodes.FirstOrDefault(node => !before.Contains(node.DataNode));
        SelectedTreeNodes.Clear();
        if (newFound is not null) SelectedTreeNodes.Add(newFound);
    }
}
