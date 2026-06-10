using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using NBTExplorer.Model;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTExplorer;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // This will be used by the Find functionality in the future.
    internal string? FindName { get; set; }
    internal string? FindValue { get; set; }

    // This is how we tell the UI our Dialogs changed.
    public new event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // This is how we block the main UI when something is happening. We only do this in IO-related tasks as these take the longest.
    internal bool IsBlocked
    {
        get;
        set
        {
            if (field == value) return;
            field = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
        }
    }

    public MainWindow()
    {
        // We initialize everything we'll need...
        InitializeComponent();
        InitializeFormHandlers();

        // ...including the Clipboard (if not null)... 
        if (Clipboard is not null) NbtClipboardController.Initialize(new NbtClipboardControllerAvalonia(Clipboard));

        // ...the TreeView's TreeNode collections...
        TreeNodes = [];
        SelectedTreeNodes = [];

        // ...the Recent Files/Folders...
        var recentItems = RecentItem.Load(true);
        RecentFiles = new ObservableCollection<RecentItem>(recentItems.Where(x => !x.IsFolder));
        RecentFolders = new ObservableCollection<RecentItem>(recentItems.Where(x => x.IsFolder));

        // ...our SelectionChanged event (for the TreeView)...
        SelectedTreeNodes.CollectionChanged += async (_, _) => await SelectionChanged();

        // Here's where the main app's logic is. All the AppCommands! Oh, and here you can also tell when my comments started losing their personality... I'm sorry, it got tiring. :C

        // This one is executed when the user chooses to Open a File through the Button.
        OpenFile = CreateAppCommand(async recentItemObject =>
        {
            var recentItem = recentItemObject as RecentItem;
            var path = recentItem?.Path;

            if (path is null)
            {
                // First we open a FilePicker, using the same FileTypeFilters as the original NBTExplorer 
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    FileTypeFilter =
                    [
                        FilePickerFileTypes.All,
                        new FilePickerFileType("NBT Files")
                        {
                            Patterns = ["*.dat", "*.schematic"]
                        },
                        new FilePickerFileType("Region Files")
                        {
                            Patterns = ["*.mca", "*.mcr"]
                        }
                    ]
                });

                // If the user didn't select any File, we pretend nothing happened...
                if (files.Count < 1) return;

                // ...but if they did select a File, we get its absolute Path... 
                path = files[0].Path.LocalPath;
            }

            // We check if the file still exists...
            if (!File.Exists(path))
            {
                // ...and if not, we remove it from our recents...
                RecentItem.Remove(path);
                if (recentItem is not null) RecentFiles.Remove(recentItem);

                // ...and tell the user.
                throw new UserErrorException($"File Not Found: {path}");
            }

            // ...then we pass it to the OpenFileAsync function, which does the actual Opening.
            await OpenFileAsync(path);
        }, StorageProvider.CanOpen);

        // This one is executed when the user chooses to Open a Folder through the Button.
        OpenFolder = CreateAppCommand(async recentItemObject =>
        {
            var recentItem = recentItemObject as RecentItem;
            var path = recentItem?.Path;

            if (path is null)
            {
                // First we open a FolderPicker.
                var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());

                // If the user didn't select any Folder, we pretend nothing happened...
                if (folders.Count < 1) return;

                // ...but if they did select a Folder, we get its absolute Path... 
                path = folders[0].Path.LocalPath;
            }

            // We check if the folder still exists...
            if (!Directory.Exists(path))
            {
                // ...and if not, we remove it from our recents...
                RecentItem.Remove(path);
                if (recentItem is not null) RecentFolders.Remove(recentItem);

                // ...and tell the user.
                throw new UserErrorException($"Directory Not Found: {path}");
            }

            // ...then we pass it to the OpenFolderAsync function, which does the actual Opening.
            await OpenFolderAsync(path);
        }, StorageProvider.CanPickFolder);

        // This one is executed when the user chooses to Open a DirectoryDataNode in their file Explorer.
        OpenInExplorer = CreateAppCommand(async _ =>
        {
            // Check if NodeDirPath is null.
            var selectedNodeDirPath =
                (SelectedTreeNodes.FirstOrDefault()?.DataNode as DirectoryDataNode)?.NodeDirPath;
            if (selectedNodeDirPath is null) throw new UnreachableException();

            // We just get its Path and use some Avalonia magic to do the actual Opening.
            await Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(selectedNodeDirPath));
        });

        // This one is executed when the user chooses to Save their "project" (TreeNodes).
        Save = CreateAppCommand(_ =>
        {
            // We iterate through all open TreeNodes...
            foreach (var node in TreeNodes)
            {
                // ...and the actual Saving is dealt with by NBTModel, convenient!
                node.DataNode.Save();
            }
        });

        // This one is executed when the user chooses to Refresh a TreeNode.
        Refresh = CreateAppCommand(async _ =>
        {
            IsBlocked = true;

            try
            {
                // Check if DataNode is null.
                var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
                if (selectedTreeNode?.DataNode is null) throw new UnreachableException();

                // First we back up the IsExpanded (UI-wise) TreeNodes.
                var savedExpandedNodes = selectedTreeNode.SaveExpandedNodes();

                // Then NBTModel deals with the main TreeNode Refreshing...
                if (!selectedTreeNode.DataNode.RefreshNode()) throw new UnreachableException();

                // ...and we deal with its children.
                await selectedTreeNode.RefreshChildNodesAsync();

                // Then we can restore the IsExpanded (UI-wise) backup...
                selectedTreeNode.RestoreExpandedNodes(savedExpandedNodes);

                // ...and clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();
            }
            finally
            {
                IsBlocked = false;
            }
        });

        // This one is executed when the user chooses to Exit through the Button.
        Exit = CreateAppCommand(_ =>
        {
            // This code was borrowed from Avalonia itself!
            switch (Application.Current)
            {
                case { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime lifetime }:
                    lifetime.TryShutdown();
                    break;
                case { ApplicationLifetime: IControlledApplicationLifetime controlledLifetime }:
                    controlledLifetime.Shutdown();
                    break;
            }
        }, true);

        // This one is executed when the user chooses to Cut a TreeNode.
        Cut = CreateAppCommand(async _ =>
        {
            // Check if DataNode or Parent are null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.DataNode is null || selectedTreeNode.Parent is null)
                throw new UnreachableException();

            // We isolate the parent because its child is going to be Cut...
            var parent = selectedTreeNode.Parent.Parent ?? TreeNodes.FirstOrDefault();

            // ...then we Cut the selected TreeNode. 
            if (!await selectedTreeNode.DataNode.CutNode()) throw new UnreachableException();

            // Then we refresh the TreeNode's parent...
            if (parent is not null) await parent.RefreshChildNodesAsync();

            // ...and also clear the SelectedTreeNodes, as the child is gone now.
            SelectedTreeNodes.Clear();
        });

        // This one is executed when the user chooses to Copy a TreeNode.
        Copy = CreateAppCommand(async _ =>
        {
            // Check if DataNode is null, and copy it if not...
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.DataNode is null || !await selectedTreeNode.DataNode.CopyNode())
                throw new UnreachableException();
        });

        // This one is executed when the user chooses to Paste a TreeNode.
        Paste = CreateAppCommand(async _ =>
        {
            // Check if DataNode is null, and paste the copied TreeNode into the selected Parent if not...
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.DataNode is null || !await selectedTreeNode.DataNode.PasteNode())
                throw new UnreachableException();

            // ...then we refresh the TreeNode's grandparent to make sure the title is accurate.
            var grandParent = selectedTreeNode.Parent ?? TreeNodes.FirstOrDefault();
            if (grandParent is not null) await grandParent.RefreshChildNodesAsync();

            // ...and clear the SelectedTreeNodes, as they're invalid now.
            SelectedTreeNodes.Clear();
        });

        // This one is executed when the user chooses to Rename a TreeNode.
        Rename = CreateAppCommand(_ =>
        {
            // Check if DataNode is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.DataNode is null) throw new UnreachableException();

            var state = new EditTagDialogState(this, true);
            OpenDialog(state);
        });

        // This one is executed when the user chooses to Edit a TreeNode.
        EditValue = CreateAppCommand(_ =>
        {
            // Check if DataNode is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.DataNode is null) throw new UnreachableException();

            var state = new EditTagDialogState(this);
            OpenDialog(state);
        });

        // This one is executed when the user chooses to Delete a TreeNode.
        Delete = CreateAppCommand(async _ =>
        {
            var grandparents = new HashSet<TreeNode?>();

            // We iterate through all SelectedTreeNodes...
            foreach (var selectedTreeNode in SelectedTreeNodes.ToList())
            {
                // ...and the actual deleting is dealt with by NBTModel, convenient!
                if (!selectedTreeNode.DataNode.DeleteNode()) throw new UnreachableException();

                // We make sure we don't refresh the same grandparent twice.
                grandparents.Add(selectedTreeNode.Parent?.Parent ?? TreeNodes.FirstOrDefault());
            }

            // We do have to deal with refreshing the grandparent ourselves, though...
            foreach (var grandparent in grandparents.OfType<TreeNode>())
            {
                await grandparent.RefreshChildNodesAsync();
            }

            // ...and also clear the SelectedTreeNodes, as the child is gone now.
            SelectedTreeNodes.Clear();
        });

        // This one is executed when the user chooses to Move Up a TreeNode.
        MoveUp = CreateAppCommand(async _ =>
        {
            // Check if the parent's SubNodes are null, and move the child if not...
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.Parent?.SubNodes is null || !selectedTreeNode.DataNode.ChangeRelativePosition(-1))
                throw new UnreachableException();

            // ...then we make sure this change is translated to the UI, after checking if indexSelected is valid...
            var indexSelected = selectedTreeNode.Parent.SubNodes.IndexOf(selectedTreeNode);
            if (indexSelected < 0) throw new UnreachableException();
            selectedTreeNode.Parent.SubNodes.Move(indexSelected, indexSelected - 1);

            // ...and we also tell the UI we did such change.
            await SelectionChanged();
        });

        // This one is executed when the user chooses to Move Down a TreeNode.
        MoveDown = CreateAppCommand(async _ =>
        {
            // Check if the parent's SubNodes are null, and move the child if not...
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.Parent?.SubNodes is null || !selectedTreeNode.DataNode.ChangeRelativePosition(1))
                throw new UnreachableException();

            // ...then we make sure this change is translated to the UI, after checking if indexSelected is valid...
            var indexSelected = selectedTreeNode.Parent.SubNodes.IndexOf(selectedTreeNode);
            if (indexSelected < 0) throw new UnreachableException();
            selectedTreeNode.Parent.SubNodes.Move(indexSelected, indexSelected + 1);

            // ...and we also tell the UI we did such change.
            await SelectionChanged();
        });

        Find = CreateAppCommand(_ => OpenDialog(new FindDialogState(this, FindName, FindValue)));

        /* TODO: Implement Find Next */
        FindNext = CreateAppCommand(_ =>
            throw new NotImplementedException(
                "Find functionality, including the FindNext AppCommand, isn't implemented yet."));

        /* TODO: Implement Replace */
        Replace = CreateAppCommand(_ =>
            throw new NotImplementedException(
                "Find functionality, including the Replace AppCommand, isn't implemented yet."));

        /* TODO: Implement Chunk Finder */
        ChunkFinder = CreateAppCommand(_ =>
            throw new NotImplementedException(
                "Find functionality, including the ChunkFinder AppCommand, isn't implemented yet."));

        // This one is executed when the user chooses to learn about us. <3
        About = CreateAppCommand(_ => OpenDialog(new AboutDialogState()), true);

        // This one is executed when the user chooses to see the NOTICE file.
        Acknowledgements =
            CreateAppCommand(
                _ => Launcher.LaunchUriAsync(
                    new Uri("https://github.com/neoNBTExplorer/neoNBTExplorer/blob/master/NOTICE.md")), true);

        // These are executed when the user chooses to Add a Tag.
        AddByteTag = CreateAppCommand(_ => AddTag(TagType.TAG_BYTE));
        AddShortTag = CreateAppCommand(_ => AddTag(TagType.TAG_SHORT));
        AddIntTag = CreateAppCommand(_ => AddTag(TagType.TAG_INT));
        AddLongTag = CreateAppCommand(_ => AddTag(TagType.TAG_LONG));
        AddFloatTag = CreateAppCommand(_ => AddTag(TagType.TAG_FLOAT));
        AddDoubleTag = CreateAppCommand(_ => AddTag(TagType.TAG_DOUBLE));
        AddByteArrayTag = CreateAppCommand(_ => AddTag(TagType.TAG_BYTE_ARRAY));
        AddIntArrayTag = CreateAppCommand(_ => AddTag(TagType.TAG_INT_ARRAY));
        AddLongArrayTag = CreateAppCommand(_ => AddTag(TagType.TAG_LONG_ARRAY));
        AddStringTag = CreateAppCommand(_ => AddTag(TagType.TAG_STRING));
        AddListTag = CreateAppCommand(_ => AddTag(TagType.TAG_LIST));
        AddCompoundTag = CreateAppCommand(_ => AddTag(TagType.TAG_COMPOUND));

        // This one is executed when the user chooses to Expand or Collapse a TreeNode.
        ToggleExpand = CreateAppCommand(_ =>
        {
            // Check if IsExpanded is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.IsExpanded is null) throw new UnreachableException();

            selectedTreeNode.IsExpanded = !selectedTreeNode.IsExpanded;
        });

        // This one is executed when the user chooses to Expand a TreeNode's Children.
        ExpandChildren = CreateAppCommand(_ =>
        {
            // Check if SubNodes is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.SubNodes is null) throw new UnreachableException();

            foreach (var child in selectedTreeNode.SubNodes)
            {
                child.IsExpanded = true;
            }
        });

        // This one is executed when the user chooses to Expand a TreeNode's Tree.
        ExpandTree = CreateAppCommand(async _ =>
        {
            // Check if selectedTreeNode is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode is null) throw new UnreachableException();

            await selectedTreeNode.ExpandTreeAsync();
        });

        // This one is executed when the user OKs a Dialog.
        DialogOk = CreateAppCommand(async _ =>
        {
            // Execute the designated OK code!
            if (_currentDialog is not null) await _currentDialog.ExecuteAsync();

            // Once the Dialog-specific actions are done, we can Refresh the Selected TreeNode's Title just in case... 
            SelectedTreeNodes.FirstOrDefault()?.RefreshTitle();

            // ...then close the Dialog.
            CloseDialog();
        });

        // This one is executed when the user Cancels a Dialog.
        DialogCancel = CreateAppCommand(_ => CloseDialog());

        // This one is executed when the user chooses to Import a new Tag Value.
        DialogImport = CreateAppCommand(async _ =>
        {
            // Check if DataNode is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.DataNode is null) throw new UnreachableException();

            // First we get the TreeNode's type...
            var tagDataNode = selectedTreeNode.DataNode as TagDataNode;
            var tagType = tagDataNode?.Tag.GetTagType();
            // ...and build an extension for it.
            var nodePath = selectedTreeNode.DataNode.NodePath.TrimStart('/', '\\');
            var extension = $".{tagType}";

            // We open a FilePicker that only accepts that extension.
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import to " + nodePath,
                FileTypeFilter =
                [
                    new FilePickerFileType($"neoNBTExplorer {tagType} Data File")
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
            if (Ascii.IsValid(fileContent))
            {
                (CurrentDialog as EditTagDialogState)?.TagValue = fileContent;
            }
            else
            {
                // ...if it isn't, it'd crash the whole app so we won't accept it.
                throw new UserErrorException(
                    "Invalid (non-ASCII) data file. Please only import data files created through neoNBTExplorer. If you did so, your file may be corrupted.");
            }
        });

        DialogExport = CreateAppCommand(async _ =>
        {
            // Check if DataNode is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode?.DataNode is null) throw new UnreachableException();

            // First we get the TreeNode's type...
            var tagDataNode = selectedTreeNode.DataNode as TagDataNode;
            var tagType = tagDataNode?.Tag.GetTagType();
            // ...and build an extension for it.
            var nodePath = selectedTreeNode.DataNode.NodePath.TrimStart('/', '\\');
            var extension = $".{tagType}";

            // We open a SaveFilePicker that only accepts that extension.
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export " + nodePath,
                FileTypeChoices =
                [
                    new FilePickerFileType($"neoNBTExplorer {tagType} Data File")
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
            await streamWriter.WriteAsync((CurrentDialog as EditTagDialogState)?.TagValue);
        });

        // Because of our pseudo-MVVM approach, we need to bind the DataContext.
        DataContext = this;
    }
}