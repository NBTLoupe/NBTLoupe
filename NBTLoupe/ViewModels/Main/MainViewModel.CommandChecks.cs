using System.IO;
using System.Linq;
using NBTLoupe.ViewModels.Dialogs;
using NBTModel.Data.Nodes;
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

    // TODO: Make CanPasteIntoNode synchronous.
    private bool CanPaste => ClipboardAvailable &&
                             SingleSelectedTreeNode?.DataNode.CanPasteIntoNode().GetAwaiter().GetResult() ==
                             true;

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

    private bool CanDialogImport => CurrentDialog is EditTagDialogViewModel { ValueVisible: true };

    private bool CanDialogExport => CurrentDialog is EditTagDialogViewModel { ValueVisible: true };
}
