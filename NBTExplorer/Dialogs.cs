using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NBTExplorer.Model;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTExplorer;

// And here's the actual Dialog definitions! If some comments are in a different style, that's because I moved them from MainWindow.axaml.cs. Sorry!
// Here we define the AddTag Dialog!
internal class AddTagDialogState : DialogState
{
    // We need to access the Window somehow!
    private readonly MainWindow _window;

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

    // The Tag Size TextBox...
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

    // Here we set up the Dialog!
    internal AddTagDialogState(MainWindow window, TagType tagType)
    {
        DialogTagType = tagType;
        _window = window;

        // Set the context-accurate Title and Type.
        TitleText = $"Add {MainWindow.GetFriendlyTag(DialogTagType)}";
    }

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
    // We need to access the Window somehow!
    private readonly MainWindow _window;

    // Here's all the fields we bind to in the XAML...
    // The Title TextBlock...
    internal string TitleText { get; }

    // The old Name and Value...
    private readonly string _oldTagName;
    private readonly string _oldTagValue;

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

    // Here we set up the Dialog!
    internal EditTagDialogState(MainWindow window)
    {
        _window = window;
        var selectedTreeNode = window.SelectedTreeNodes.FirstOrDefault();
        var nodeName = selectedTreeNode?.DataNode.NodeName;
        var tagDataNode = selectedTreeNode?.DataNode as TagDataNode;
        DialogTagType = tagDataNode?.Tag.GetTagType() ?? TagType.TAG_END;

        // Set the context-accurate Title and Type.
        TitleText = $"Edit {MainWindow.GetFriendlyTag(DialogTagType)}";

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
        };
        TagValue = _oldTagValue;
    }

    // And here's where our Validation magic happens!
    internal override bool IsOkEnabled
    {
        get
        {
            var node = _window.SelectedTreeNodes.FirstOrDefault()?.DataNode;

            if (!node.CanEditNode) return !string.IsNullOrEmpty(TagName) && _oldTagName != TagName;

            return !string.IsNullOrEmpty(TagName) && !string.IsNullOrEmpty(TagValue)
            && _oldTagName != TagName && _oldTagValue != TagValue;
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
                TagType.TAG_STRING => FormRegistry.EditString(new StringFormData("")),

                TagType.TAG_BYTE => FormRegistry.EditTagScalar(new TagScalarFormData(new TagNodeByte())),
                TagType.TAG_SHORT => FormRegistry.EditTagScalar(new TagScalarFormData(new TagNodeShort())),
                TagType.TAG_INT => FormRegistry.EditTagScalar(new TagScalarFormData(new TagNodeInt())),
                TagType.TAG_LONG => FormRegistry.EditTagScalar(new TagScalarFormData(new TagNodeLong())),
                TagType.TAG_FLOAT => FormRegistry.EditTagScalar(new TagScalarFormData(new TagNodeFloat())),
                TagType.TAG_DOUBLE => FormRegistry.EditTagScalar(new TagScalarFormData(new TagNodeDouble())),

                TagType.TAG_BYTE_ARRAY or TagType.TAG_SHORT_ARRAY or TagType.TAG_INT_ARRAY
                    or TagType.TAG_LONG_ARRAY => FormRegistry.EditByteArray(new ByteArrayFormData()),

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

        // Check if DataNode or tag is null.
        var node = selectedTreeNode?.DataNode;
        if (node is null) throw new UnreachableException();
        if ((node as TagDataNode)?.Tag is null) throw new UnreachableException();

        // ...we let the FormHandlers deal with it.
        if (node.CanEditNode && !node.EditNode()) throw new UnreachableException();
        if (_oldTagName != TagName && !node.RenameNode()) throw new UnreachableException();

        // And we refresh its parent so the order updates.
        if (selectedTreeNode.Parent is not null) await selectedTreeNode.Parent.RefreshChildNodesAsync();
    }
}

// Here we define the Find Dialog!
/* TODO: Actually implementing it! */
internal class FindDialogState : DialogState
{
    // We need to access the Window somehow!
    private readonly MainWindow _window;

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
    // Here's all the fields we bind to in the XAML...
    // The Exception TextBlock...
    internal string ExceptionText { get; }

    // ...whether to suggest to open an issue...
    internal bool PotentialBug { get; }

    // ...and whether the user should be forced to restart the app.
    internal bool FatalException { get; }

    // This is just how we force the user to restart the app, by not letting them close the Dialog!
    internal override bool IsOkEnabled => !FatalException;

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