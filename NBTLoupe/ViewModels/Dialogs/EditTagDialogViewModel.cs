using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NBTLoupe.Core;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data;
using NBTModel.Data.Nodes;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the EditTag Dialog!
internal partial class EditTagDialogViewModel : DialogHostViewModel
{
    // The old Name and Value...
    private readonly string _oldTagName;

    private readonly string _oldTagValue;

    // Here we set up the Dialog!
    internal EditTagDialogViewModel(MainViewModel mainViewModel, bool isRename = false) : base(mainViewModel)
    {
        IsRename = isRename;

        SpecialButtons = ValueVisible
            ? [new DialogButton("Import", DialogImportCommand), new DialogButton("Export", DialogExportCommand)]
            : [];

        var tagDataNode = MainViewModel.SingleSelectedTreeNode?.DataNode as TagDataNode;
        DialogTagType = tagDataNode?.Tag.GetTagType() ?? TagType.TAG_END;

        // If the TreeNode is a NbtFileDataNode, its Renameable Name is different.
        _oldTagName = (MainViewModel.SingleSelectedTreeNode?.DataNode is not NbtFileDataNode fileDataNode
            ? MainViewModel.SingleSelectedTreeNode?.DataNode?.NodeName
            : fileDataNode.TreeName) ?? "";
        TagName = _oldTagName;

        // Set the context-accurate Title and Type.
        TitleText =
            $"Edit {TreeNode.GetFriendlyTag(tagDataNode?.Tag.GetTagType())}{(!string.IsNullOrEmpty(_oldTagName) ? $": \"{_oldTagName}\"" : " Value")}";

        // If the TreeNode is an Array, we parse it depending on which kind it is.
        _oldTagValue = tagDataNode?.Tag.GetTagType() switch
        {
            TagType.TAG_BYTE_ARRAY => string.Join(",", tagDataNode.Tag.ToTagByteArray().Data),
            TagType.TAG_SHORT_ARRAY => string.Join(",", tagDataNode.Tag.ToTagShortArray().Data),
            TagType.TAG_INT_ARRAY => string.Join(",", tagDataNode.Tag.ToTagIntArray().Data),
            TagType.TAG_LONG_ARRAY => string.Join(",", tagDataNode.Tag.ToTagLongArray().Data),
            _ => tagDataNode?.Tag.ToString()
        } ?? "";
        TagValue = _oldTagValue;
    }

    // This is so we focus on the Name TextBox if clicking the Rename button!
    internal bool IsRename { get; }

    // Here's all the fields we bind to in the XAML...
    // The Title TextBlock...
    internal string TitleText { get; }

    // The new Name TextBox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? TagName { get; set; }

    // ...(which is only visible in certain cases, by the way)
    internal bool NameVisible => MainViewModel.SingleSelectedTreeNode?.DataNode.CanRenameNode ?? false;

    // ... and the new Value TextBox
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? TagValue { get; set; }

    // ...(which is only visible in certain cases, by the way)
    internal bool ValueVisible => MainViewModel.SingleSelectedTreeNode?.DataNode.CanEditNode ?? false;

    // And here's where our Validation magic happens!
    internal override bool IsOkEnabled
    {
        get
        {
            // Only enable the OK button if:
            // - The use inputted a new Name or Value.
            // - The new Name is valid for the corresponding TagType.
            // - The new Value is valid for the corresponding TagType.
            var hasNewTagName = _oldTagName != TagName;
            var hasNewTagValue = _oldTagValue != TagValue;

            if (!hasNewTagName && !hasNewTagValue) return false;

            var tagNode = MainViewModel.SingleSelectedTreeNode?.DataNode;
            var tagDataNode = tagNode as TagDataNode;
            var metaTagContainer = tagDataNode?.Parent as IMetaTagContainer;

            bool? valid = null;

            if (hasNewTagName)
                valid = valid is null or true && _oldTagName != TagName &&
                        (!string.IsNullOrEmpty(TagName) || tagNode is NbtFileDataNode) &&
                        (metaTagContainer?.NamedTagContainer is null ||
                         !metaTagContainer.NamedTagContainer.TagNamesInUse.Contains(TagName));

            if (hasNewTagValue)
                valid = valid is null or true && tagDataNode?.Tag is not null &&
                        ValidateTagValue(tagDataNode.Tag.GetTagType());

            return valid ?? false;
        }
    }

    // This gives the OK button tailor-made text!
    internal override string OkText => "Edit";

    // This allows us to have special separate buttons for Import/Export!
    internal override IReadOnlyList<DialogButton> SpecialButtons { get; }

    // This one is executed when the user chooses to Import a new Tag Value.
    [RelayCommand]
    private Task<bool> DialogImport()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            // Check if DataNode is null.
            if (MainViewModel.SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

            // First we get the TreeNode's type...
            var tagDataNode = MainViewModel.SingleSelectedTreeNode.DataNode as TagDataNode;
            var tagType = tagDataNode?.Tag.GetTagType();
            // ...and build an extension for it.
            var nodePath = MainViewModel.SingleSelectedTreeNode.DataNode.NodePath.TrimStart('/', '\\');
            var extension = $".{tagType}";

            // We open a FilePicker that only accepts that extension.
            var files = await MainViewModel.TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import to " + nodePath,
                FileTypeFilter =
                [
                    new FilePickerFileType($"NBTLoupe {tagType} Data File")
                    {
                        Patterns = [$"*{extension}"]
                    }
                ]
            });

            // If the user didn't select any File, we pretend nothing happened.
            if (files.Count < 1) return;

            // We start reading the opened File.
            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            var fileContent = await streamReader.ReadToEndAsync();

            // If the file is Ascii, it may follow our format...
            if (Ascii.IsValid(fileContent)) TagValue = fileContent;
            else
                // ...if it isn't, it'd crash the whole app so we won't accept it.
                throw new UserErrorException(
                    "Invalid (non-ASCII) data file. Please only import data files created through NBTLoupe. If you did so, your file may be corrupted.");
        });
    }

    // This one is executed when the user chooses to Export a Tag Value.
    [RelayCommand]
    private Task<bool> DialogExport()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            // Check if DataNode is null.
            if (MainViewModel.SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

            // First we get the TreeNode's type...
            var tagDataNode = MainViewModel.SingleSelectedTreeNode.DataNode as TagDataNode;
            var tagType = tagDataNode?.Tag.GetTagType();
            // ...and build an extension for it.
            var nodePath = MainViewModel.SingleSelectedTreeNode.DataNode.NodePath.TrimStart('/', '\\');
            var extension = $".{tagType}";

            // We open a SaveFilePicker that only accepts that extension.
            var file = await MainViewModel.TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export " + nodePath,
                FileTypeChoices =
                [
                    new FilePickerFileType($"NBTLoupe {tagType} Data File")
                    {
                        Patterns = [$"*{extension}"]
                    }
                ],
                DefaultExtension = extension,
                SuggestedFileName = nodePath.Replace("/", "-").Replace("\\", "-")
            });

            // If the user didn't save any File, we pretend nothing happened.
            if (file is null) return;

            // But if they did select a File, we save the Tag's value to it.
            await using var stream = await file.OpenWriteAsync();
            await using var streamWriter = new StreamWriter(stream);
            await streamWriter.WriteAsync(TagValue);
        });
    }

    // Just kidding, it happens here, so we can still use the FormRegistry for Editing.
    private static bool ValidateTagValue(TagType tagType)
    {
        // And we let the FormRegistry deal with it! Neat, eh?
        try
        {
            return tagType switch
            {
                TagType.TAG_STRING => FormRegistry.EditString!(new StringFormData("")),

                TagType.TAG_BYTE => FormRegistry.EditTagScalar!(new TagScalarFormData(new TagNodeByte())),
                TagType.TAG_SHORT => FormRegistry.EditTagScalar!(new TagScalarFormData(new TagNodeShort())),
                TagType.TAG_INT => FormRegistry.EditTagScalar!(new TagScalarFormData(new TagNodeInt())),
                TagType.TAG_LONG => FormRegistry.EditTagScalar!(new TagScalarFormData(new TagNodeLong())),
                TagType.TAG_FLOAT => FormRegistry.EditTagScalar!(new TagScalarFormData(new TagNodeFloat())),
                TagType.TAG_DOUBLE => FormRegistry.EditTagScalar!(new TagScalarFormData(new TagNodeDouble())),

                TagType.TAG_BYTE_ARRAY or TagType.TAG_SHORT_ARRAY or TagType.TAG_INT_ARRAY
                    or TagType.TAG_LONG_ARRAY => FormRegistry.EditByteArray!(new ByteArrayFormData { Data = [] }),

                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        var dataNode = MainViewModel.SingleSelectedTreeNode?.DataNode;

        var hasNewTagName = _oldTagName != TagName;
        var hasNewTagValue = _oldTagValue != TagValue;

        var success = true;
        // ...we let the FormHandlers deal with it.
        if (hasNewTagName) success &= dataNode?.RenameNode() == true;
        if (hasNewTagValue) success &= dataNode?.EditNode() == true;

        if (!success) throw new UnreachableException();

        // Then we back up our SelectedTreeNodes' IndexPath.
        var savedSelectedTreeNodes = MainViewModel.SingleSelectedTreeNode?.GetIndexPath(MainViewModel.TreeNodes);

        // And, on a rename, we refresh its parent so the order updates.
        if (hasNewTagName && MainViewModel.SingleSelectedTreeNode?.Parent is not null)
        {
            await MainViewModel.SingleSelectedTreeNode.Parent.RefreshChildNodesAsync();

            // And finally, we restore our SelectedTreeNodes using our IndexPath and the new name.
            if (savedSelectedTreeNodes is null) return;
            var restoredSelectedTreeNode =
                TreeNode.GetByIndexPath(MainViewModel.TreeNodes, savedSelectedTreeNodes);
            var foundNode =
                restoredSelectedTreeNode?.Parent?.SubNodes?.FirstOrDefault(node => node.DataNode.NodeName == TagName);
            if (foundNode is not null) MainViewModel.SelectedTreeNodes.Add(foundNode);
        }
    }
}
