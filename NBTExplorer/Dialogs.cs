using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NBTModel.Data;
using NBTModel.Data.Nodes;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTExplorer;

// And here's the actual Dialog definitions! If some comments are in a different style, that's because I moved them from MainWindow.axaml.cs. Sorry!
// Here we define the AddTag Dialog!
internal class AddTagDialogState : DialogState
{
    // We need to access the Window somehow!
    private readonly MainWindow _window;

    // Here we set up the Dialog!
    internal AddTagDialogState(MainWindow window, TagType tagType)
    {
        DialogTagType = tagType;
        _window = window;

        // Set the context-accurate Title and Type.
        TitleText = $"Add {MainWindow.GetFriendlyTag(DialogTagType)}";
    }

    // Here's all the fields we bind to in the XAML...
    // The Title TextBlock...
    internal string TitleText { get; }

    // The Tag Name TextBox...
    internal string? TagName
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();
        }
    }

    // The Tag Size NumericUpDown...
    internal decimal TagSize
    {
        get;
        set
        {
            field = value < 0 ? 0 : value;
            OnPropertyChanged();
        }
    }

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
            var metaTagContainer = _window.SelectedTreeNodes.FirstOrDefault()?.DataNode as IMetaTagContainer;
            return metaTagContainer?.NamedTagContainer is null ||
                   !metaTagContainer.NamedTagContainer.TagNamesInUse.Contains(TagName);
        }
    }

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        // Check if SubNodes is null, and return if so.
        var selectedTreeNode = _window.SelectedTreeNodes.FirstOrDefault();
        if (selectedTreeNode?.SubNodes is null) throw new UnreachableException();

        // Save its parent's SubNodes.
        var before = selectedTreeNode.SubNodes.Select(n => n.DataNode).ToHashSet();

        // Create the new TreeNode.
        if (!selectedTreeNode.DataNode.CreateNode(DialogTagType)) throw new UnreachableException();

        // IsExpand (UI-wise) the new TreeNode.
        selectedTreeNode.IsExpanded = true;

        // Refresh its parent.
        await selectedTreeNode.RefreshChildNodesAsync();

        // And find the new TreeNode, so we can Select it.
        _window.SelectedTreeNodes.Clear();
        var newFound = selectedTreeNode.SubNodes.FirstOrDefault(node => !before.Contains(node.DataNode));
        if (newFound is not null) _window.SelectedTreeNodes.Add(newFound);
    }
}

// Here we define the EditTag Dialog!
internal class EditTagDialogState : DialogState
{
    // The old Name and Value...
    private readonly string _oldTagName;

    private readonly string _oldTagValue;

    // We need to access the Window somehow!
    private readonly MainWindow _window;

    // Here we set up the Dialog!
    internal EditTagDialogState(MainWindow window, bool isRename = false)
    {
        _window = window;
        IsRename = isRename;

        var selectedTreeNode = window.SelectedTreeNodes.FirstOrDefault();
        var nodeName = selectedTreeNode?.DataNode.NodeName;
        var tagDataNode = selectedTreeNode?.DataNode as TagDataNode;
        DialogTagType = tagDataNode?.Tag.GetTagType() ?? TagType.TAG_END;

        // Set the context-accurate Title and Type.
        TitleText =
            $"Edit {MainWindow.GetFriendlyTag(tagDataNode?.Tag.GetTagType())} {(!string.IsNullOrEmpty(nodeName) ? $": \"{nodeName}\"" : "Value")}";

        // If the TreeNode is a NbtFileDataNode, its Renameable Name is different.
        _oldTagName = (selectedTreeNode?.DataNode is not NbtFileDataNode fileDataNode
            ? nodeName
            : fileDataNode.TreeName) ?? "";
        TagName = _oldTagName;

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
    internal string? TagName
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();
        }
    }

    // ...(which is only visible in certain cases, by the way)
    internal bool NameVisible => _window.SelectedTreeNodes.FirstOrDefault()?.DataNode.CanRenameNode ?? false;

    // ... and the new Value TextBox
    internal string? TagValue
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();
        }
    }

    // ...(which is only visible in certain cases, by the way)
    internal bool ValueVisible => _window.SelectedTreeNodes.FirstOrDefault()?.DataNode.CanEditNode ?? false;

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

            var tagNode = _window.SelectedTreeNodes.FirstOrDefault()?.DataNode;
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
        var selectedTreeNode = _window.SelectedTreeNodes.FirstOrDefault();
        var dataNode = selectedTreeNode?.DataNode;

        var hasNewTagName = _oldTagName != TagName;
        var hasNewTagValue = _oldTagValue != TagValue;

        var success = false;
        // ...we let the FormHandlers deal with it.
        if (hasNewTagName) success = dataNode?.RenameNode() == true;
        if (hasNewTagValue) success = dataNode?.EditNode() == true;

        if (!success) throw new UnreachableException();

        // Then we back up our SelectedTreeNodes' IndexPath.
        var savedSelectedTreeNodes = selectedTreeNode?.GetIndexPath(_window.TreeNodes);

        // And, on a rename, we refresh its parent so the order updates.
        if (hasNewTagName && selectedTreeNode?.Parent is not null)
        {
            await selectedTreeNode.Parent.RefreshChildNodesAsync();

            // And finally, we restore our SelectedTreeNodes using our IndexPath and the new name.
            if (savedSelectedTreeNodes is null) return;
            var restoredSelectedTreeNode =
                MainWindow.TreeNode.GetByIndexPath(_window.TreeNodes, savedSelectedTreeNodes);
            var foundNode =
                restoredSelectedTreeNode?.Parent?.SubNodes?.FirstOrDefault(node => node.DataNode.NodeName == TagName);
            if (foundNode is not null) _window.SelectedTreeNodes.Add(foundNode);
        }
    }
}

// Here we define the Find Dialog!
/* TODO: Actually implementing it! */
internal class FindDialogState : DialogState
{
    // We need to access the Window somehow!
    private readonly MainWindow _window;

    // Here we set up the Dialog!
    internal FindDialogState(MainWindow window, string? findName, string? findValue)
    {
        _window = window;

        // We restore the values from the MainWindow.
        NameEnabled = findName is not null;
        NameText = findName;
        ValueEnabled = findValue is not null;
        ValueText = findValue;
    }

    // Here's all the fields we bind to in the XAML...
    // The Name CheckBox...
    internal bool NameEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();
        }
    }

    // The Value Checkbox...
    internal bool ValueEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();
        }
    }

    // The Name TextBox...
    internal string? NameText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();
        }
    }

    // The Value TextBox...
    internal string? ValueText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();
        }
    }

    // And here's where our Validation magic happens!
    // Only enable the OK button if:
    // - At least one TextBox has its CheckBox enabled. 
    // - The user inputted something into that TextBox.
    internal override bool IsOkEnabled => (NameEnabled && !string.IsNullOrEmpty(NameText)) ||
                                          (ValueEnabled && !string.IsNullOrEmpty(ValueText));

    // And here's the actual magic! The OK button!
    internal override Task ExecuteAsync()
    {
        // We set the enabled values into our MainWindow variable, to potentially use elsewhere.
        _window.FindName = NameEnabled ? NameText : null;
        _window.FindValue = ValueEnabled ? ValueText : null;

        // Then we make sure the user knows this is not done yet!
        throw new NotImplementedException("Find functionality, including the Find Dialog, isn't implemented yet.");
    }
}

// Here we define the ChunkFinder Dialog!
internal class ChunkFinderDialogState : DialogState
{
    // We need to access the Window somehow!
    private readonly MainWindow _window;

    // We don't want to cascade into infinite updates when the user inputs something!
    private bool _isUpdating;

    // Here we set up the Dialog!
    internal ChunkFinderDialogState(MainWindow window)
    {
        _window = window;
    }

    // Here's all the fields we bind to in the XAML...
    // The UI locker...
    internal bool InProgress
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.OnPropertyChanged(nameof(_window.ShowProgressBar));
            _window.RefreshOkButton();
        }
    }

    // The Region X's Placeholder TextBox...
    internal string? RegionXPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "Type here";

    // The Region X's NumericUpDown...
    internal string? RegionX
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var regionX)) return;
                ChunkXPlaceholder = $"({regionX * 32} to {(regionX + 1) * 32 - 1})";
                BlockXPlaceholder = $"({regionX * 32 * 16} to {(regionX + 1) * 32 * 16 - 1})";
                LocalChunkXPlaceholder = "(0 to 31)";
                LocalBlockXPlaceholder = "(0 to 15)";

                if (!int.TryParse(LocalChunkX, out var localChunkX)) return;
                ChunkX = (regionX * 32 + localChunkX).ToString();
                if (int.TryParse(LocalBlockX, out var localBlockX))
                    BlockX = (regionX * 32 * 16 + localChunkX * 16 + localBlockX).ToString();
                else
                    BlockXPlaceholder =
                        $"({(regionX * 32 + localChunkX) * 16} to {(regionX * 32 + localChunkX + 1) * 16 - 1})";
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    } = "0";

    // The Region Z's Placeholder TextBox...
    internal string? RegionZPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "Type here";

    // The Region Z's NumericUpDown...
    internal string? RegionZ
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var regionZ)) return;
                ChunkZPlaceholder = $"({regionZ * 32} to {(regionZ + 1) * 32 - 1})";
                BlockZPlaceholder = $"({regionZ * 32 * 16} to {(regionZ + 1) * 32 * 16 - 1})";
                LocalChunkZPlaceholder = "(0 to 31)";
                LocalBlockZPlaceholder = "(0 to 15)";

                if (!int.TryParse(LocalChunkZ, out var localChunkZ)) return;
                ChunkZ = (regionZ * 32 + localChunkZ).ToString();
                if (int.TryParse(LocalBlockZ, out var localBlockZ))
                    BlockZ = (regionZ * 32 * 16 + localChunkZ * 16 + localBlockZ).ToString();
                else
                    BlockZPlaceholder =
                        $"({(regionZ * 32 + localChunkZ) * 16} to {(regionZ * 32 + localChunkZ + 1) * 16 - 1})";
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    } = "0";

    // The Chunk X's Placeholder TextBox...
    internal string? ChunkXPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 31)";

    // The Chunk X's NumericUpDown...
    internal string? ChunkX
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var chunkX)) return;
                RegionX = (chunkX >> 5).ToString();

                if (int.TryParse(LocalBlockX, out var localBlockX)) BlockX = (chunkX * 16 + localBlockX).ToString();
                LocalChunkX = ((chunkX % 32 + 32) % 32).ToString();

                BlockXPlaceholder = $"({chunkX * 16} to {(chunkX + 1) * 16 - 1})";
                LocalBlockXPlaceholder = "(0 to 15)";
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // The Chunk Z's Placeholder TextBox...
    internal string? ChunkZPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 31)";

    // The Chunk Z's NumericUpDown...
    internal string? ChunkZ
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var chunkZ)) return;
                RegionZ = (chunkZ >> 5).ToString();

                if (int.TryParse(LocalBlockZ, out var localBlockZ)) BlockZ = (chunkZ * 16 + localBlockZ).ToString();
                LocalChunkZ = ((chunkZ % 32 + 32) % 32).ToString();

                BlockZPlaceholder = $"({chunkZ * 16} to {(chunkZ + 1) * 16 - 1})";
                LocalBlockZPlaceholder = "(0 to 15)";
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // The Block X's Placeholder TextBox...
    internal string? BlockXPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 511)";

    // The Block X's NumericUpDown...
    internal string? BlockX
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var blockX)) return;
                RegionX = (blockX >> 4 >> 5).ToString();
                ChunkX = (blockX >> 4).ToString();
                LocalChunkX = (((blockX >> 4) % 32 + 32) % 32).ToString();
                LocalBlockX = ((blockX % 16 + 16) % 16).ToString();
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // The Block Z's Placeholder TextBox...
    internal string? BlockZPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 511)";

    // The Block Z's NumericUpDown...
    internal string? BlockZ
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var blockZ)) return;
                RegionZ = (blockZ >> 4 >> 5).ToString();
                ChunkZ = (blockZ >> 4).ToString();
                LocalChunkZ = (((blockZ >> 4) % 32 + 32) % 32).ToString();
                LocalBlockZ = ((blockZ % 16 + 16) % 16).ToString();
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // The Local Chunk X's Placeholder TextBox...
    internal string? LocalChunkXPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 31)";

    // The Local Chunk X's NumericUpDown...
    internal string? LocalChunkX
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var localChunkX)) return;
                RegionXPlaceholder = "(ANY)";
                ChunkXPlaceholder = "(ANY)";
                BlockXPlaceholder = "(ANY)";
                LocalBlockXPlaceholder = "(0 to 15)";

                if (!int.TryParse(RegionX, out var regionX)) return;
                ChunkX = (regionX * 32 + localChunkX).ToString();

                if (int.TryParse(LocalBlockX, out var localBlockX))
                    BlockX = (regionX * 32 * 16 + localChunkX * 16 + localBlockX).ToString();
                else
                    BlockXPlaceholder =
                        $"({(regionX * 32 + localChunkX) * 16} to {(regionX * 32 + localChunkX + 1) * 16 - 1})";
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // The Local Chunk Z's Placeholder TextBox...
    internal string? LocalChunkZPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 31)";

    // The Local Chunk Z's NumericUpDown...
    internal string? LocalChunkZ
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var localChunkZ)) return;
                RegionZPlaceholder = "(ANY)";
                ChunkZPlaceholder = "(ANY)";
                BlockZPlaceholder = "(ANY)";
                LocalBlockZPlaceholder = "(0 to 15)";

                if (!int.TryParse(RegionZ, out var regionZ)) return;
                ChunkZ = (regionZ * 32 + localChunkZ).ToString();

                if (int.TryParse(LocalBlockZ, out var localBlockZ))
                    BlockZ = (regionZ * 32 * 16 + localChunkZ * 16 + localBlockZ).ToString();
                else
                    BlockZPlaceholder =
                        $"({(regionZ * 32 + localChunkZ) * 16} to {(regionZ * 32 + localChunkZ + 1) * 16 - 1})";
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // The Local Block X's Placeholder TextBox...
    internal string? LocalBlockXPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 15)";

    // The Local Block X's NumericUpDown...
    internal string? LocalBlockX
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var localBlockX)) return;
                RegionXPlaceholder = "(ANY)";
                ChunkXPlaceholder = "(ANY)";
                BlockXPlaceholder = "(ANY)";

                if (!int.TryParse(RegionX, out var regionX) || !int.TryParse(LocalChunkX, out var localChunkX)) return;
                ChunkX = (regionX * 32 + localChunkX).ToString();
                BlockX = (regionX * 32 * 16 + localChunkX * 16 + localBlockX).ToString();
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // The Local Block Z's Placeholder TextBox...
    internal string? LocalBlockZPlaceholder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "(0 to 15)";

    // The Local Block Z's NumericUpDown...
    internal string? LocalBlockZ
    {
        get;
        set
        {
            field = int.TryParse(value, out _) ? value : field;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOkEnabled));
            _window.RefreshOkButton();

            // We make sure we don't trigger cascading updates.
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                if (!int.TryParse(value, out var localBlockZ)) return;
                RegionZPlaceholder = "(ANY)";
                ChunkZPlaceholder = "(ANY)";
                BlockZPlaceholder = "(ANY)";

                if (!int.TryParse(RegionZ, out var regionZ) || !int.TryParse(LocalChunkZ, out var localChunkZ)) return;
                ChunkZ = (regionZ * 32 + localChunkZ).ToString();
                BlockZ = (regionZ * 32 * 16 + localChunkZ * 16 + localBlockZ).ToString();
            }
            finally
            {
                // And we finished!
                _isUpdating = false;
            }
        }
    }

    // And here's where our Validation magic happens!
    internal override bool IsOkEnabled =>
        !InProgress && !string.IsNullOrEmpty(LocalChunkX) && !string.IsNullOrEmpty(LocalChunkZ);

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        InProgress = true;

        var selectedTreeNode = _window.SelectedTreeNodes.FirstOrDefault();

        if (selectedTreeNode is null || !int.TryParse(RegionX, out var regionX) ||
            !int.TryParse(RegionZ, out var regionZ) ||
            !int.TryParse(LocalChunkX, out var localChunkX) ||
            !int.TryParse(LocalChunkZ, out var localChunkZ)) throw new UnreachableException();

        var foundNode = await selectedTreeNode.SearchAsync(regionX, regionZ, localChunkX, localChunkZ);
        if (foundNode is null) return;

        await foundNode.ExpandTreeReverseAsync();
        _window.SelectedTreeNodes.Clear();
        _window.SelectedTreeNodes.Add(foundNode);
    }
}

// Here we define the About Dialog!
internal class AboutDialogState : DialogState
{
    // We set the AboutDialog's title from here as it's neater to have the current version in there.
    internal static string AboutTitle => $"About {Program.FullName}";

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }
}

// Here we define the Error Dialog! (:C)
internal class UserErrorException(string message) : Exception(message);

internal class ErrorDialogState : DialogState
{
    // Here we set up the Dialog!
    internal ErrorDialogState(Exception exception, bool fatal = false)
    {
        // If the Exception is fatal, we force the user to restart the app.
        FatalException = fatal;

        // If it's one of these Exceptions, it likely isn't a bug, but rather a user-caused Error. So we don't want to confuse the user into opening an issue.
        PotentialBug = exception is not UserErrorException && exception is not NotImplementedException &&
                       !FatalException;

        // This is just so people running NAOT builds (AKA everyone on RELEASE) don't get a confusing StackTrace. 
        ExceptionText = RuntimeFeature.IsDynamicCodeSupported ? exception.ToString() : exception.Message;
    }

    // Here's all the fields we bind to in the XAML...
    // The Exception TextBlock...
    internal string ExceptionText { get; }

    // ...whether to suggest to open an issue...
    internal bool PotentialBug { get; }

    // ...and whether the user should be forced to restart the app.
    internal bool FatalException { get; }

    // This is just how we force the user to restart the app, by not letting them close the Dialog!
    internal override bool IsOkEnabled => !FatalException;

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }
}

// Here we define the UnsavedChanges Dialog!
internal class UnsavedChangesDialogState : DialogState
{
    // We need to access the Window somehow!
    private readonly MainWindow _window;

    // Here we set up the Dialog!
    internal UnsavedChangesDialogState(MainWindow window)
    {
        _window = window;
    }

    // And here's the actual magic! The OK button!
    internal override Task ExecuteAsync()
    {
        // We disable the Save button to bypass the dialog...
        _window.Save.Toggle(false);
        // ...and immediately exit!
        _window.Exit.Execute(null);

        return Task.CompletedTask;
    }
}
