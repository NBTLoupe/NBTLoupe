using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the AddTag Dialog!
internal partial class AddTagDialogViewModel : DialogHostViewModel
{
    // We need to access the MainViewModel somehow!
    private readonly MainViewModel _viewModel;

    // Here we set up the Dialog!
    internal AddTagDialogViewModel(MainViewModel viewModel, TagType tagType)
    {
        DialogTagType = tagType;
        _viewModel = viewModel;

        // Set the context-accurate Title and Type.
        TitleText = $"Add {TreeNode.GetFriendlyTag(DialogTagType)}";
    }

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
    internal bool SizeEnabled => DialogTagType is TagType.TAG_BYTE_ARRAY or TagType.TAG_SHORT_ARRAY
        or TagType.TAG_INT_ARRAY or TagType.TAG_LONG_ARRAY;

    // And here's where our Validation magic happens!
    internal override bool IsOkEnabled
    {
        get
        {
            // Only enable the OK button if:
            // - The use inputted a Name.
            // - There isn't already a sibling with that same Name.
            if (string.IsNullOrEmpty(TagName)) return false;
            var metaTagContainer = _viewModel.SingleSelectedTreeNode?.DataNode as IMetaTagContainer;
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
    internal override async Task ExecuteAsync()
    {
        // Check if SubNodes is null, and return if so.
        if (_viewModel.SingleSelectedTreeNode?.SubNodes is null) throw new UnreachableException();

        // Save its parent's SubNodes.
        var before = _viewModel.SingleSelectedTreeNode.SubNodes.Select(n => n.DataNode).ToHashSet();

        // Create the new TreeNode.
        if (!_viewModel.SingleSelectedTreeNode.DataNode.CreateNode(DialogTagType)) throw new UnreachableException();

        // IsExpand (UI-wise) the new TreeNode.
        _viewModel.SingleSelectedTreeNode.IsExpanded = true;

        // Refresh its parent.
        await _viewModel.SingleSelectedTreeNode.RefreshChildNodesAsync();

        // And find the new TreeNode, so we can Select it.
        _viewModel.SelectedTreeNodes.Clear();
        var newFound =
            _viewModel.SingleSelectedTreeNode.SubNodes.FirstOrDefault(node => !before.Contains(node.DataNode));
        if (newFound is not null) _viewModel.SelectedTreeNodes.Add(newFound);
    }
}
