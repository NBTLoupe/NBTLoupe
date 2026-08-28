using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
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
    public MainViewModel()
    {
        // Because SelectedTreeNodes is a Collection, we require this EventHandler for our RelayCommands to notice any change.
        SelectedTreeNodes?.CollectionChanged += (_, _) =>
        {
            SingleSelectedTreeNode = SelectedTreeNodes.Count == 1 ? SelectedTreeNodes[0] : null;
        };
    }

    // Here's where the main app's logic is. All the RelayCommands! Oh, and here you can also tell when my comments started losing their personality... I'm sorry, it got tiring. :C

    // This one is executed when the user chooses to Open a File through the Button.
    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private Task OpenFile(RecentItem? recentItem)
    {
        return SafeExecuteAsync(async () =>
        {
            var path = recentItem?.Path;

            if (path is null)
            {
                // First we open a FilePicker, using the same FileTypeFilters as the original NBTExplorer 
                var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
            await Opener.OpenFileAsync(this, path);
        });
    }

    // This one is executed when the user chooses to Open a Folder through the Button.
    [RelayCommand(CanExecute = nameof(CanOpenFolder))]
    private Task OpenFolder(RecentItem? recentItem)
    {
        return SafeExecuteAsync(async () =>
        {
            var path = recentItem?.Path;

            if (path is null)
            {
                // First we open a FolderPicker.
                var folders = await TopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());

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
            await Opener.OpenFolderAsync(this, path);
        });
    }

    // This one is executed when the user Drops a File/Folder into the app.
    [RelayCommand]
    private Task DropFile(string path)
    {
        return SafeExecuteAsync(async () =>
        {
            if (Directory.Exists(path)) await Opener.OpenFolderAsync(this, path);
            else if (File.Exists(path)) await Opener.OpenFileAsync(this, path);
        });
    }

    // This one is executed when the user chooses to Open their Minecraft Save Folder.
    [RelayCommand(CanExecute = nameof(CanOpenMinecraftSaveFolder))]
    private Task OpenMinecraftSaveFolder()
    {
        return SafeExecuteAsync(async () =>
        {
            // ...we pass the Folder to the OpenFolderAsync function, which does the actual Opening.
            await Opener.OpenFolderAsync(this, Program.MinecraftSaveFolder);
        });
    }

    // This one is executed when the user chooses to Open a DirectoryDataNode in their file Explorer.
    [RelayCommand(CanExecute = nameof(CanOpenInExplorer))]
    private Task OpenInExplorer()
    {
        return SafeExecuteAsync(async () =>
        {
            // Check if NodeDirPath is null.
            var selectedNodeDirPath =
                (SingleSelectedTreeNode?.DataNode as DirectoryDataNode)?.NodeDirPath;
            if (selectedNodeDirPath is null) throw new UnreachableException();

            // We just get its Path and use some Avalonia magic to do the actual Opening.
            await TopLevel.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(selectedNodeDirPath));
        });
    }

    // This one is executed when the user chooses to Save their "project" (TreeNodes).
    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task Save()
    {
        return SafeExecuteAsync(async () =>
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
    }

    // This one is executed when the user chooses to Refresh a TreeNode.
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private Task Refresh()
    {
        return SafeExecuteAsync(async () =>
        {
            // If the user has unsaved changes...
            var shouldContinue = !CanSave;

            // ...we open a Dialog to warn them.
            if (!shouldContinue)
                shouldContinue = await OpenDialogAsync(new UnsavedChangesDialogViewModel(this));

            // And if the user Cancelled, we return.
            if (!shouldContinue) return;

            await WithBlock(async () =>
            {
                // Check if DataNode is null.
                if (SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

                // First we back up the IsExpanded (UI-wise) TreeNodes.
                var savedExpandedNodes = SingleSelectedTreeNode.SaveExpandedNodes();

                // Then we back up our SelectedTreeNodes' IndexPath.
                var savedSelectedTreeNodes = SingleSelectedTreeNode.GetIndexPath(TreeNodes);

                // Then we unIsExpand the SelectedTreeNode, to allow for its lazy-loading.
                SingleSelectedTreeNode.IsExpanded = false;

                // Then NBTModel deals with the main TreeNode Refreshing...
                if (!SingleSelectedTreeNode.DataNode.RefreshNode()) throw new UnreachableException();

                // ...and we deal with its children....
                await SingleSelectedTreeNode.RefreshChildNodesAsync();

                // ...making sure they're lazy-loaded.
                await SingleSelectedTreeNode.LazyLoadAsync();

                // Then we can restore the IsExpanded (UI-wise) backup.
                SingleSelectedTreeNode.IsExpanded = true;
                await SingleSelectedTreeNode.RestoreExpandedNodesAsync(savedExpandedNodes);

                // And clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();

                // And finally, we restore our SelectedTreeNodes using our IndexPath.
                var restoredSelectedTreeNode = TreeNode.GetByIndexPath(TreeNodes, savedSelectedTreeNodes);
                if (restoredSelectedTreeNode is not null) SelectedTreeNodes.Add(restoredSelectedTreeNode);
            });
        });
    }


    // This one is executed when the user chooses to Exit through the Button.
    [RelayCommand]
    private Task Exit()
    {
        return SafeExecuteAsync(() =>
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

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user is trying to Exit with Unsaved Changes.
    [RelayCommand]
    private Task AbortExit()
    {
        return SafeExecuteAsync(async () => { await OpenDialogAsync(new UnsavedChangesDialogViewModel(this, true)); });
    }

    // This one is executed when the user chooses to Cut a TreeNode.
    [RelayCommand(CanExecute = nameof(CanCut))]
    private Task Cut()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(async () =>
            {
                // Check if DataNode or Parent are null.
                if (SingleSelectedTreeNode?.DataNode is null || SingleSelectedTreeNode.Parent is null)
                    throw new UnreachableException();

                // We isolate the parent because its child is going to be Cut...
                var parent = SingleSelectedTreeNode.Parent ?? TreeNodes.FirstOrDefault();

                // Then we back up our SelectedTreeNodes' IndexPath.
                var savedSelectedTreeNodes = SingleSelectedTreeNode.GetIndexPath(TreeNodes);

                // ...then we Cut the selected TreeNode. 
                if (!await SingleSelectedTreeNode.DataNode.CutNode()) throw new UnreachableException();

                // Then we refresh the TreeNode's parent...
                if (parent is not null) await parent.RefreshChildNodesAsync();

                // And clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();

                // And finally, we restore our SelectedTreeNodes using our IndexPath.
                var restoredSelectedTreeNode = TreeNode.GetByIndexPath(TreeNodes, savedSelectedTreeNodes);
                if (restoredSelectedTreeNode is not null) SelectedTreeNodes.Add(restoredSelectedTreeNode);
            });
        });
    }

    // This one is executed when the user chooses to Copy a TreeNode.
    [RelayCommand(CanExecute = nameof(CanCopy))]
    private Task Copy()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(async () =>
            {
                // Check if DataNode is null, and copy it if not...
                if (SingleSelectedTreeNode?.DataNode is null || !await SingleSelectedTreeNode.DataNode.CopyNode())
                    throw new UnreachableException();
            });
        });
    }

    // This one is executed when the user chooses to Paste a TreeNode.
    [RelayCommand(CanExecute = nameof(CanPaste))]
    private Task Paste()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(async () =>
            {
                // Check if DataNode is null...
                if (SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

                // Then we back up our SelectedTreeNodes' IndexPath.
                var savedSelectedTreeNodes = SingleSelectedTreeNode.GetIndexPath(TreeNodes);

                // ...and paste the copied TreeNode into the selected Parent if not...
                if (!await SingleSelectedTreeNode.DataNode.PasteNode()) throw new UnreachableException();

                // ...then we refresh the parent.
                await SingleSelectedTreeNode.RefreshChildNodesAsync();

                // And clear the SelectedTreeNodes, as they're invalid now.
                SelectedTreeNodes.Clear();

                // And finally, we restore our SelectedTreeNodes using our IndexPath.
                var restoredSelectedTreeNode = TreeNode.GetByIndexPath(TreeNodes, savedSelectedTreeNodes);
                if (restoredSelectedTreeNode is not null) SelectedTreeNodes.Add(restoredSelectedTreeNode);
            });
        });
    }

    // This one is executed when the user chooses to Rename a TreeNode.
    [RelayCommand(CanExecute = nameof(CanRename))]
    private Task Rename()
    {
        return SafeExecuteAsync(async () =>
        {
            // Check if DataNode is null.
            if (SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

            await OpenDialogAsync(new EditTagDialogViewModel(this, true));
        });
    }

    // This one is executed when the user chooses to Edit a TreeNode.
    [RelayCommand(CanExecute = nameof(CanEditValue))]
    private Task EditValue()
    {
        return SafeExecuteAsync(async () =>
        {
            // Check if DataNode is null.
            if (SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

            await OpenDialogAsync(new EditTagDialogViewModel(this));
        });
    }

    // This is a mixed version of Rename and EditValue, which is used to Focus on the right TextBox when unknown. 
    [RelayCommand]
    private Task EditOrRename()
    {
        return SafeExecuteAsync(async () =>
        {
            if (CanEditValue) await EditValueCommand.ExecuteAsync(null);
            else if (CanRename) await RenameCommand.ExecuteAsync(null);
        });
    }

    // This one is executed when the user chooses to Delete a TreeNode.
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task Delete()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(async () =>
            {
                var parents = new HashSet<TreeNode?>();

                // We back up the last SelectedTreeNode's IndexPath.
                var savedSelectedTreeNodes = SelectedTreeNodes.LastOrDefault()?.GetIndexPath(TreeNodes);

                // We iterate through all SelectedTreeNodes...
                foreach (var selectedTreeNode in SelectedTreeNodes.ToList())
                {
                    // ...and the actual deleting is dealt with by NBTModel, convenient!
                    if (!selectedTreeNode.DataNode.DeleteNode()) throw new UnreachableException();

                    // We make sure we don't refresh the same parent twice.
                    parents.Add(selectedTreeNode.Parent ?? TreeNodes.FirstOrDefault());
                }

                // We do have to deal with refreshing the parent ourselves, though...
                foreach (var parent in parents.OfType<TreeNode>()) await parent.RefreshChildNodesAsync();

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
    }

    // This one is executed when the user chooses to Move Up a TreeNode.
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private Task MoveUp()
    {
        return SafeExecuteAsync(() =>
        {
            // Check if the parent's SubNodes are null, and move the child if not...
            if (SingleSelectedTreeNode?.Parent?.SubNodes is null ||
                !SingleSelectedTreeNode.DataNode.ChangeRelativePosition(-1))
                throw new UnreachableException();

            // ...then we make sure this change is translated to the UI, after checking if indexSelected is valid...
            var indexSelected = SingleSelectedTreeNode.Parent.SubNodes.IndexOf(SingleSelectedTreeNode);
            if (indexSelected < 0) throw new UnreachableException();
            SingleSelectedTreeNode.Parent.SubNodes.Move(indexSelected, indexSelected - 1);

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Move Down a TreeNode.
    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private Task MoveDown()
    {
        return SafeExecuteAsync(() =>
        {
            // Check if the parent's SubNodes are null, and move the child if not...
            if (SingleSelectedTreeNode?.Parent?.SubNodes is null ||
                !SingleSelectedTreeNode.DataNode.ChangeRelativePosition(1))
                throw new UnreachableException();

            // ...then we make sure this change is translated to the UI, after checking if indexSelected is valid...
            var indexSelected = SingleSelectedTreeNode.Parent.SubNodes.IndexOf(SingleSelectedTreeNode);
            if (indexSelected < 0) throw new UnreachableException();
            SingleSelectedTreeNode.Parent.SubNodes.Move(indexSelected, indexSelected + 1);

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to open a Find Dialog.
    [RelayCommand(CanExecute = nameof(CanFind))]
    private Task Find()
    {
        return SafeExecuteAsync(async () =>
        {
            // We first create the Dialog...
            var dialogViewModel = new FindReplaceDialogViewModel(this);

            // ...then we open it, and wait for the results. If we didn't find anything...
            if (await OpenDialogAsync(dialogViewModel) && !dialogViewModel.FoundMatch)
                // ...we tell the user.
                await OpenDialogAsync(new InfoDialogViewModel("No matching tags were found."));
        });
    }

    // This one is executed when the user chooses to continue their pre-started Find operation.
    [RelayCommand(CanExecute = nameof(CanFindNext))]
    private Task FindNext()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(async () =>
            {
                // Check if the BasicSearcher is null, which it shouldn't if the AppCommand is enabled.
                if (BasicSearcher is null) throw new UnreachableException();

                // Then we Find the next instance of the searched parameters.
                var found = await BasicSearcher.FindNextAsync();

                // If we didn't Find anything... 
                if (found is null)
                {
                    // ...we clean our BasicSearcher...
                    BasicSearcher = null;

                    // ...we disable the FindNext AppCommand...
                    EnableFindNext = false;

                    // ...and open a Dialog telling the user about this.
                    await OpenDialogAsync(new InfoDialogViewModel("End of results."));
                    return;
                }

                // But if we did Find something, we start Expanding its tree in reverse.
                await found.ExpandTreeReverseAsync();

                // This is so, when we add it to SelectedTreeNodes, the UI automatically jumps to it.
                SelectedTreeNodes.Clear();
                SelectedTreeNodes.Add(found);
            });
        });
    }

    // This one is executed when the user chooses to open a Replace Dialog (AKA an Advanced mode Find Dialog).
    // TODO: This kind of mode isn't implemented yet!
    [RelayCommand(CanExecute = nameof(CanReplace))]
    private Task Replace()
    {
        return SafeExecuteAsync(async () => { await OpenDialogAsync(new FindReplaceDialogViewModel(this, true)); });
    }

    // This one is executed when the user chooses to open a ChunkFinder Dialog.
    [RelayCommand(CanExecute = nameof(CanChunkFinder))]
    private Task ChunkFinder()
    {
        return SafeExecuteAsync(async () =>
        {
            // We first create the Dialog...
            var dialogViewModel = new ChunkFinderDialogViewModel(this);

            // ...then we open it, and wait for the results. If we didn't find anything...
            if (await OpenDialogAsync(dialogViewModel) && !dialogViewModel.FoundMatch)
                // ...we tell the user.
                await OpenDialogAsync(new InfoDialogViewModel("Chunk not found."));
        });
    }

    // This one is executed when the user chooses to learn about us. <3
    [RelayCommand]
    private Task About()
    {
        return SafeExecuteAsync(async () => { await OpenDialogAsync(new AboutDialogViewModel()); });
    }

    // This one is executed when the user chooses to see the NOTICE file.
    [RelayCommand]
    private Task Acknowledgements()
    {
        return SafeExecuteAsync(async () =>
        {
            await TopLevel.Launcher.LaunchUriAsync(
                new Uri("https://github.com/NBTLoupe/NBTLoupe/blob/master/NOTICE.md"));
        });
    }

    // These are executed when the user chooses to Add a Tag.
    [RelayCommand(CanExecute = nameof(CanAddByteTag))]
    private Task AddByteTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_BYTE); });
    }

    [RelayCommand(CanExecute = nameof(CanAddShortTag))]
    private Task AddShortTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_SHORT); });
    }

    [RelayCommand(CanExecute = nameof(CanAddIntTag))]
    private Task AddIntTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_INT); });
    }

    [RelayCommand(CanExecute = nameof(CanAddLongTag))]
    private Task AddLongTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_LONG); });
    }

    [RelayCommand(CanExecute = nameof(CanAddFloatTag))]
    private Task AddFloatTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_FLOAT); });
    }

    [RelayCommand(CanExecute = nameof(CanAddDoubleTag))]
    private Task AddDoubleTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_DOUBLE); });
    }

    [RelayCommand(CanExecute = nameof(CanAddByteArrayTag))]
    private Task AddByteArrayTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_BYTE_ARRAY); });
    }

    [RelayCommand(CanExecute = nameof(CanAddIntArrayTag))]
    private Task AddIntArrayTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_INT_ARRAY); });
    }

    [RelayCommand(CanExecute = nameof(CanAddLongArrayTag))]
    private Task AddLongArrayTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_LONG_ARRAY); });
    }

    [RelayCommand(CanExecute = nameof(CanAddStringTag))]
    private Task AddStringTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_STRING); });
    }

    [RelayCommand(CanExecute = nameof(CanAddListTag))]
    private Task AddListTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_LIST); });
    }

    [RelayCommand(CanExecute = nameof(CanAddCompoundTag))]
    private Task AddCompoundTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_COMPOUND); });
    }

    // This one is executed when the user chooses to Expand or Collapse a TreeNode.
    [RelayCommand(CanExecute = nameof(CanToggleExpand))]
    private Task ToggleExpand()
    {
        return SafeExecuteAsync(() =>
        {
            // Check if IsExpanded is null.
            if (SingleSelectedTreeNode?.IsExpanded is null) throw new UnreachableException();

            SingleSelectedTreeNode.IsExpanded = !SingleSelectedTreeNode.IsExpanded;

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Expand a TreeNode's Children.
    [RelayCommand(CanExecute = nameof(CanExpandChildren))]
    private Task ExpandChildren()
    {
        return SafeExecuteAsync(() =>
        {
            // Check if SubNodes is null.
            if (SingleSelectedTreeNode?.SubNodes is null) throw new UnreachableException();

            foreach (var child in SingleSelectedTreeNode.SubNodes) child.IsExpanded = true;

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Expand a TreeNode's Tree.
    [RelayCommand(CanExecute = nameof(CanExpandTree))]
    private Task ExpandTree()
    {
        return SafeExecuteAsync(async () =>
        {
            // Check if selectedTreeNode is null.
            if (SingleSelectedTreeNode is null) throw new UnreachableException();

            await WithBlock(async () => await SingleSelectedTreeNode.ExpandTreeAsync(), true);
        });
    }

    // This one is executed when the user chooses to Import a new Tag Value.
    [RelayCommand(CanExecute = nameof(CanDialogImport))]
    private Task DialogImport()
    {
        return SafeExecuteAsync(async () =>
        {
            // Check if DataNode is null.
            if (SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

            // First we get the TreeNode's type...
            var tagDataNode = SingleSelectedTreeNode.DataNode as TagDataNode;
            var tagType = tagDataNode?.Tag.GetTagType();
            // ...and build an extension for it.
            var nodePath = SingleSelectedTreeNode.DataNode.NodePath.TrimStart('/', '\\');
            var extension = $".{tagType}";

            // We open a FilePicker that only accepts that extension.
            var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
            if (Ascii.IsValid(fileContent))
                (CurrentDialog as EditTagDialogViewModel)?.TagValue = fileContent;
            else
                // ...if it isn't, it'd crash the whole app so we won't accept it.
                throw new UserErrorException(
                    "Invalid (non-ASCII) data file. Please only import data files created through NBTLoupe. If you did so, your file may be corrupted.");
        });
    }

    // This one is executed when the user chooses to Export a Tag Value.
    [RelayCommand(CanExecute = nameof(CanDialogExport))]
    private Task DialogExport()
    {
        return SafeExecuteAsync(async () =>
        {
            // Check if DataNode is null.
            if (SingleSelectedTreeNode?.DataNode is null) throw new UnreachableException();

            // First we get the TreeNode's type...
            var tagDataNode = SingleSelectedTreeNode.DataNode as TagDataNode;
            var tagType = tagDataNode?.Tag.GetTagType();
            // ...and build an extension for it.
            var nodePath = SingleSelectedTreeNode.DataNode.NodePath.TrimStart('/', '\\');
            var extension = $".{tagType}";

            // We open a SaveFilePicker that only accepts that extension.
            var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
            await streamWriter.WriteAsync((CurrentDialog as EditTagDialogViewModel)?.TagValue);
        });
    }

    // This allows us to easily catch any errors!
    private async Task SafeExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            // If the exception comes from Substrate, things are probably on fire. That's fatal.
            var fatal = e is SubstrateException;

            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Write(fatal ? LogEventLevel.Fatal : LogEventLevel.Error, e,
                "[NBTLoupe]: RelayCommand exception");

            await OpenDialogAsync(new ErrorDialogViewModel(e, fatal));
        }
        finally
        {
            // Oh, and it also allows us to easily tell the Save Button to update!
            SaveCommand.NotifyCanExecuteChanged();
        }
    }
}
