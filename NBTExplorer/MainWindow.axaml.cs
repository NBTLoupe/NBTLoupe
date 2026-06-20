using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using NBTModel.Data.Nodes;
using NBTModel.Interop;
using Serilog;
using Substrate.Nbt;

namespace NBTExplorer;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // This helps us set the variable above only when it's truly needed. So if an operation is unlikely to take long, we can make sure the UI is only locked if it is taking exceptionally long. This prevents flashing the UI. 
    private int _blockDepth;

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
        SelectedTreeNodes.CollectionChanged += async (_, _) =>
        {
            try
            {
                await SelectionChanged();
            }
            catch (Exception ex)
            {
                // If something goes wrong, we log it and show a Dialog to the user. :C
                Log.Error(ex, "[neoNBTExplorer]: Unhandled UI thread exception");
                OpenDialog(new ErrorDialogState(ex));
            }
        };

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

        // This one is executed when the user chooses to Open their Minecraft Save Folder.
        OpenMinecraftSaveFolder = CreateAppCommand(async _ =>
        {
            // ...we pass the Folder to the OpenFolderAsync function, which does the actual Opening.
            await OpenFolderAsync(Program.MinecraftSaveFolder);
        }, Directory.Exists(Program.MinecraftSaveFolder));

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
        Save = CreateAppCommand(async _ =>
        {
            await WithBlock(() =>
            {
                // We iterate through all open TreeNodes...
                foreach (var node in TreeNodes)
                    // ...and the actual Saving is dealt with by NBTModel, convenient!
                    node.DataNode.Save();

                return Task.CompletedTask;
            });
        });

        // This one is executed when the user chooses to Refresh a TreeNode.
        Refresh = CreateAppCommand(async _ =>
        {
            await WithBlock(async () =>
            {
                // Check if DataNode is null.
                var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
                if (selectedTreeNode?.DataNode is null) throw new UnreachableException();

                // First we back up the IsExpanded (UI-wise) TreeNodes.
                var savedExpandedNodes = selectedTreeNode.SaveExpandedNodes();

                // Then we back up our SelectedTreeNodes' IndexPath.
                var savedSelectedTreeNodes = selectedTreeNode.GetIndexPath(TreeNodes);

                // Then we unIsExpand the SelectedTreeNode, to allow for its lazy-loading.
                selectedTreeNode.IsExpanded = false;

                // Then NBTModel deals with the main TreeNode Refreshing...
                if (!selectedTreeNode.DataNode.RefreshNode()) throw new UnreachableException();

                // ...and we deal with its children....
                await selectedTreeNode.RefreshChildNodesAsync();

                // ...making sure they're lazy-loaded.
                await selectedTreeNode.LazyLoadAsync();

                // Then we can restore the IsExpanded (UI-wise) backup.
                selectedTreeNode.IsExpanded = true;
                await selectedTreeNode.RestoreExpandedNodes(savedExpandedNodes);

                // And clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();

                // And finally, we restore our SelectedTreeNodes using our IndexPath.
                var restoredSelectedTreeNode = TreeNode.GetByIndexPath(TreeNodes, savedSelectedTreeNodes);
                if (restoredSelectedTreeNode is not null) SelectedTreeNodes.Add(restoredSelectedTreeNode);
            });
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
            await WithBlock(async () =>
            {
                // Check if DataNode or Parent are null.
                var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
                if (selectedTreeNode?.DataNode is null || selectedTreeNode.Parent is null)
                    throw new UnreachableException();

                // We isolate the parent because its child is going to be Cut...
                var parent = selectedTreeNode.Parent.Parent ?? TreeNodes.FirstOrDefault();

                // Then we back up our SelectedTreeNodes' IndexPath.
                var savedSelectedTreeNodes = selectedTreeNode.GetIndexPath(TreeNodes);

                // ...then we Cut the selected TreeNode. 
                if (!await selectedTreeNode.DataNode.CutNode()) throw new UnreachableException();

                // Then we refresh the TreeNode's parent...
                if (parent is not null) await parent.RefreshChildNodesAsync();

                // And clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();

                // And finally, we restore our SelectedTreeNodes using our IndexPath.
                var restoredSelectedTreeNode = TreeNode.GetByIndexPath(TreeNodes, savedSelectedTreeNodes);
                if (restoredSelectedTreeNode is not null) SelectedTreeNodes.Add(restoredSelectedTreeNode);
            });
        });

        // This one is executed when the user chooses to Copy a TreeNode.
        Copy = CreateAppCommand(async _ =>
        {
            await WithBlock(async () =>
            {
                // Check if DataNode is null, and copy it if not...
                var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
                if (selectedTreeNode?.DataNode is null || !await selectedTreeNode.DataNode.CopyNode())
                    throw new UnreachableException();
            });
        });

        // This one is executed when the user chooses to Paste a TreeNode.
        Paste = CreateAppCommand(async _ =>
        {
            await WithBlock(async () =>
            {
                // Check if DataNode is null...
                var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
                if (selectedTreeNode?.DataNode is null) throw new UnreachableException();

                // Then we back up our SelectedTreeNodes' IndexPath.
                var savedSelectedTreeNodes = selectedTreeNode.GetIndexPath(TreeNodes);

                // ...and paste the copied TreeNode into the selected Parent if not...
                if (!await selectedTreeNode.DataNode.PasteNode()) throw new UnreachableException();

                // ...then we refresh the TreeNode's grandparent to make sure the title is accurate.
                var grandParent = selectedTreeNode.Parent ?? TreeNodes.FirstOrDefault();
                if (grandParent is not null) await grandParent.RefreshChildNodesAsync();

                // And clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();

                // And finally, we restore our SelectedTreeNodes using our IndexPath.
                var restoredSelectedTreeNode = TreeNode.GetByIndexPath(TreeNodes, savedSelectedTreeNodes);
                if (restoredSelectedTreeNode is not null) SelectedTreeNodes.Add(restoredSelectedTreeNode);
            });
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
            await WithBlock(async () =>
            {
                var grandparents = new HashSet<TreeNode?>();

                // We back up the last SelectedTreeNode's IndexPath.
                var savedSelectedTreeNodes = SelectedTreeNodes.LastOrDefault()?.GetIndexPath(TreeNodes);

                // We iterate through all SelectedTreeNodes...
                foreach (var selectedTreeNode in SelectedTreeNodes.ToList())
                {
                    // ...and the actual deleting is dealt with by NBTModel, convenient!
                    if (!selectedTreeNode.DataNode.DeleteNode()) throw new UnreachableException();

                    // We make sure we don't refresh the same grandparent twice.
                    grandparents.Add(selectedTreeNode.Parent?.Parent ?? TreeNodes.FirstOrDefault());
                }

                // We do have to deal with refreshing the grandparent ourselves, though...
                foreach (var grandparent in grandparents.OfType<TreeNode>()) await grandparent.RefreshChildNodesAsync();

                // And clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();

                // And finally, we restore our SelectedTreeNodes using our IndexPath.
                if (savedSelectedTreeNodes is not null)
                {
                    var restoredSelectedTreeNode = TreeNode.GetByIndexPath(TreeNodes, savedSelectedTreeNodes);
                    if (restoredSelectedTreeNode is not null) SelectedTreeNodes.Add(restoredSelectedTreeNode);
                }
            });
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

        ChunkFinder = CreateAppCommand(_ => OpenDialog(new ChunkFinderDialogState(this)));

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

            foreach (var child in selectedTreeNode.SubNodes) child.IsExpanded = true;
        });

        // This one is executed when the user chooses to Expand a TreeNode's Tree.
        ExpandTree = CreateAppCommand(async _ =>
        {
            // Check if selectedTreeNode is null.
            var selectedTreeNode = SelectedTreeNodes.FirstOrDefault();
            if (selectedTreeNode is null) throw new UnreachableException();

            await WithBlock(async () => await selectedTreeNode.ExpandTreeAsync(), true);
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
                (CurrentDialog as EditTagDialogState)?.TagValue = fileContent;
            else
                // ...if it isn't, it'd crash the whole app so we won't accept it.
                throw new UserErrorException(
                    "Invalid (non-ASCII) data file. Please only import data files created through neoNBTExplorer. If you did so, your file may be corrupted.");
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

    // This will be used by the Find functionality in the future.
    internal string? FindName { get; set; }
    internal string? FindValue { get; set; }

    // This is how we block the main UI when something is happening.
    internal bool IsBlocked
    {
        get;
        set
        {
            if (field == value) return;
            field = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowProgressBar)));
        }
    }

    // And this is just so Dialogs (which also block the UI) don't show a progress bar.
    internal bool ShowProgressBar => IsBlocked && CurrentDialog is null or ChunkFinderDialogState { InProgress: true };

    // This is how we tell the UI our Dialogs changed.
    public new event PropertyChangedEventHandler? PropertyChanged;

    internal void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async Task WithBlock(Func<Task> execute, bool usuallySlow = false)
    {
        // We create a CancellationTokenSource...
        using var source = new CancellationTokenSource();

        // ...and use its token here.
        _ = Task.Delay(usuallySlow ? 0 : 250, source.Token).ContinueWith(_ => IsBlocked = true, source.Token);

        // This allows us to have several WithBlocks run at once, and not unblock the UI early.
        _blockDepth++;

        try
        {
            // Here we execute.
            await execute();
        }
        finally
        {
            // And once it finishes, we cancel our CancellationTokenSource. Making sure our UI is never blocked if execute took less than maxWait.
            await source.CancelAsync();

            // But if it did take more, we need to make sure we unblock it.
            if (--_blockDepth < 1) IsBlocked = false;
        }
    }
}
