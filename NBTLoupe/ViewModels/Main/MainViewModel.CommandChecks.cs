using System;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core;
using NBTLoupe.ViewModels.Dialogs;
using NBTModel.Data.Nodes;
using Serilog;
using Serilog.Events;
using Substrate;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Main;

public partial class MainViewModel
{
    // I honestly don't feel like commenting this, it's extremely self-explanatory. All the RelayCommands related to these are in another file.

    private static bool CanOpenFile => TopLevel.StorageProvider.CanOpen;

    private static bool CanOpenFolder => TopLevel.StorageProvider.CanPickFolder;

    private static bool CanOpenMinecraftSaveFolder => Directory.Exists(Program.MinecraftSaveFolder);

    private bool CanOpenInExplorer => SingleSelectedTreeNode?.DataNode is DirectoryDataNode;

    internal bool CanSave => !DisableSave && TopLevel.StorageProvider.CanSave &&
                             TreeNodes.Any(node => node.DataNode.IsModified);

    private bool CanRefresh => SingleSelectedTreeNode?.DataNode.CanRefreshNode ?? false;

    private bool CanCut => ClipboardAvailable && SingleSelectedTreeNode?.DataNode.CanCutNode == true;

    private bool CanCopy => ClipboardAvailable && SingleSelectedTreeNode?.DataNode.CanCopyNode == true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PasteCommand))]
    private partial bool CanPasteCached { get; set; }

    private bool CanPaste => CanPasteCached;

    internal bool CanRename => SingleSelectedTreeNode?.DataNode.CanRenameNode ?? false;

    internal bool CanEditValue => SingleSelectedTreeNode?.DataNode.CanEditNode ?? false;

    private bool CanDelete => SelectedTreeNodes.Count > 0 && SelectedTreeNodes.All(x => x.DataNode.CanDeleteNode);

    private bool CanMoveUp => SingleSelectedTreeNode?.DataNode is
        { CanReoderNode: true, CanMoveNodeUp: true };

    private bool CanMoveDown => SingleSelectedTreeNode?.DataNode is
        { CanReoderNode: true, CanMoveNodeDown: true };

    private bool CanFind => SingleSelectedTreeNode?.DataNode.CanSearchNode ?? false;

    private bool CanFindNext => EnableFindNext;

    private bool CanReplace => SingleSelectedTreeNode?.DataNode.CanSearchNode ?? false;

    private bool CanChunkFinder =>
        SingleSelectedTreeNode?.DataNode is DirectoryDataNode or RegionFileDataNode or RegionChunkDataNode;

    private bool CanAddByteTag => SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_BYTE) ?? false;

    private bool CanAddShortTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_SHORT) ?? false;

    private bool CanAddIntTag => SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_INT) ?? false;

    private bool CanAddLongTag => SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_LONG) ?? false;

    private bool CanAddFloatTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_FLOAT) ?? false;

    private bool CanAddDoubleTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_DOUBLE) ?? false;

    private bool CanAddByteArrayTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_BYTE_ARRAY) ?? false;

    private bool CanAddIntArrayTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_INT_ARRAY) ?? false;

    private bool CanAddLongArrayTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_LONG_ARRAY) ?? false;

    private bool CanAddStringTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_STRING) ?? false;

    private bool CanAddListTag => SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_LIST) ?? false;

    private bool CanAddCompoundTag =>
        SingleSelectedTreeNode?.DataNode.CanCreateTag(TagType.TAG_COMPOUND) ?? false;

    private bool CanToggleExpand => SingleSelectedTreeNode?.SubNodes?.Count > 0;

    private bool CanExpandChildren => SingleSelectedTreeNode?.IsExpanded ?? false;

    private bool CanExpandTree => SingleSelectedTreeNode?.IsExpanded ?? false;

    // We cache the CanPaste independently, as it'd lock the UI thread otherwise.
    async partial void OnSingleSelectedTreeNodeChanged(TreeNode? value)
    {
        try
        {
            // We compute the CanPasteIntoNode value.
            var canPaste = ClipboardAvailable && value is not null && await value.DataNode.CanPasteIntoNode();

            // We make sure the SingleSelectedTreeNode hasn't changed before we computed the value.
            if (SingleSelectedTreeNode == value) CanPasteCached = canPaste;
        }
        catch (Exception e)
        {
            // If the exception comes from Substrate, things are probably on fire. That's fatal.
            var fatal = e is SubstrateException;

            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Write(fatal ? LogEventLevel.Fatal : LogEventLevel.Error, e,
                "[NBTLoupe]: CanPasteIntoNode exception");

            await OpenDialogAsync(new ErrorDialogViewModel(this, e, fatal));
        }
    }
}
