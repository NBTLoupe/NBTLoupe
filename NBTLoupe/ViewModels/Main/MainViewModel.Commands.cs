using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private Task<bool> OpenFile(RecentItem? recentItem)
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
    private Task<bool> OpenFolder(RecentItem? recentItem)
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
    private Task<bool> DropFile(string path)
    {
        return SafeExecuteAsync(async () =>
        {
            if (Directory.Exists(path)) await Opener.OpenFolderAsync(this, path);
            else if (File.Exists(path)) await Opener.OpenFileAsync(this, path);
        });
    }

    // This one is executed when the user chooses to Open their Minecraft Save Folder.
    [RelayCommand(CanExecute = nameof(CanOpenMinecraftSaveFolder))]
    private Task<bool> OpenMinecraftSaveFolder()
    {
        return SafeExecuteAsync(async () =>
        {
            // ...we pass the Folder to the OpenFolderAsync function, which does the actual Opening.
            await Opener.OpenFolderAsync(this, Program.MinecraftSaveFolder);
        });
    }

    // This one is executed when the user chooses to Open a DirectoryDataNode in their file Explorer.
    [RelayCommand(CanExecute = nameof(CanOpenInExplorer))]
    private Task<bool> OpenInExplorer()
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
    private Task<bool> Save()
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
    private Task<bool> Refresh()
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
    private Task<bool> Exit()
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
    private Task<bool> AbortExit()
    {
        return SafeExecuteAsync(async () => { await OpenDialogAsync(new UnsavedChangesDialogViewModel(this, true)); });
    }

    // This one is executed when the user chooses to Cut a TreeNode.
    [RelayCommand(CanExecute = nameof(CanCut))]
    private Task<bool> Cut()
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
    private Task<bool> Copy()
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
    private Task<bool> Paste()
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
    private Task<bool> Rename()
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
    private Task<bool> EditValue()
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
    private Task<bool> EditOrRename()
    {
        return SafeExecuteAsync(async () =>
        {
            if (CanEditValue) await EditValueCommand.ExecuteAsync(null);
            else if (CanRename) await RenameCommand.ExecuteAsync(null);
        });
    }

    // This one is executed when the user chooses to Delete a TreeNode.
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task<bool> Delete()
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
    private Task<bool> MoveUp()
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
    private Task<bool> MoveDown()
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
    private Task<bool> Find()
    {
        return SafeExecuteAsync(async () =>
        {
            // We first create the Dialog...
            var dialogViewModel = new FindReplaceDialogViewModel(this);

            // ...then we open it, and wait for the results. If we didn't find anything...
            if (await OpenDialogAsync(dialogViewModel) && !dialogViewModel.FoundMatch)
                // ...we tell the user.
                await OpenDialogAsync(new InfoDialogViewModel(this, "No matching tags were found."));
        });
    }

    // This one is executed when the user chooses to continue their pre-started Find operation.
    [RelayCommand(CanExecute = nameof(CanFindNext))]
    private Task<bool> FindNext()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(async () =>
            {
                // Check if the BasicSearcher is null, which it shouldn't if the RelayCommand is enabled.
                if (BasicSearcher is null) throw new UnreachableException();

                // Then we Find the next instance of the searched parameters.
                var found = await BasicSearcher.FindNextAsync();

                // If we didn't Find anything, we immediately return.
                if (found is null) return;

                // But if we did Find something, we start Expanding its tree in reverse.
                await found.ExpandTreeReverseAsync();

                // This is so, when we add it to SelectedTreeNodes, the UI automatically jumps to it.
                SelectedTreeNodes.Clear();
                SelectedTreeNodes.Add(found);

                // We need to tell the Find Previous Command about this as it may need to be Enabled now.
                FindPreviousCommand.NotifyCanExecuteChanged();
            });
        });
    }

    // This one is executed when the user chooses to go backwards in their pre-started Find operation.
    [RelayCommand(CanExecute = nameof(CanFindPrevious))]
    private Task<bool> FindPrevious()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(async () =>
            {
                // Check if the BasicSearcher is null, which it shouldn't if the RelayCommand is enabled.
                if (BasicSearcher is null) throw new UnreachableException();

                // Then we Find the next instance of the searched parameters.
                var found = BasicSearcher.FindPrevious();

                // If we didn't Find anything, we immediately return.
                if (found is null) return;

                // But if we did Find something, we start Expanding its tree in reverse.
                await found.ExpandTreeReverseAsync();

                // This is so, when we add it to SelectedTreeNodes, the UI automatically jumps to it.
                SelectedTreeNodes.Clear();
                SelectedTreeNodes.Add(found);

                // We need to tell the Find Next Command about this as it may need to be Enabled now.
                FindNextCommand.NotifyCanExecuteChanged();
            });
        });
    }

    // This one is executed when the user chooses to stop their pre-started Find operation.
    [RelayCommand]
    private Task<bool> FindStop()
    {
        return SafeExecuteAsync(async () =>
        {
            await WithBlock(() =>
            {
                // Yup, all we do is set the BasicSearcher to null!
                BasicSearcher = null;

                return Task.CompletedTask;
            });
        });
    }

    // This one is executed when the user chooses to open a Replace Dialog (AKA an Advanced mode Find Dialog).
    // TODO: This kind of mode isn't implemented yet!
    [RelayCommand(CanExecute = nameof(CanReplace))]
    private Task<bool> Replace()
    {
        return SafeExecuteAsync(async () => { await OpenDialogAsync(new FindReplaceDialogViewModel(this, true)); });
    }

    // This one is executed when the user chooses to open a ChunkFinder Dialog.
    [RelayCommand(CanExecute = nameof(CanChunkFinder))]
    private Task<bool> ChunkFinder()
    {
        return SafeExecuteAsync(async () =>
        {
            // We first create the Dialog...
            var dialogViewModel = new ChunkFinderDialogViewModel(this);

            // ...then we open it, and wait for the results. If we didn't find anything...
            if (await OpenDialogAsync(dialogViewModel) && !dialogViewModel.FoundMatch)
                // ...we tell the user.
                await OpenDialogAsync(new InfoDialogViewModel(this, "Chunk not found."));
        });
    }

    // This one is executed when the user chooses to learn about us. <3
    [RelayCommand]
    private Task<bool> About()
    {
        return SafeExecuteAsync(async () => { await OpenDialogAsync(new AboutDialogViewModel(this)); });
    }

    // This one is executed when the user chooses to see the NOTICE file.
    [RelayCommand]
    private Task<bool> Acknowledgements()
    {
        return SafeExecuteAsync(async () =>
        {
            await TopLevel.Launcher.LaunchUriAsync(
                new Uri("https://github.com/NBTLoupe/NBTLoupe/blob/master/NOTICE.md"));
        });
    }

    // These are executed when the user chooses to Add a Tag.
    [RelayCommand(CanExecute = nameof(CanAddByteTag))]
    private Task<bool> AddByteTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_BYTE); });
    }

    [RelayCommand(CanExecute = nameof(CanAddShortTag))]
    private Task<bool> AddShortTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_SHORT); });
    }

    [RelayCommand(CanExecute = nameof(CanAddIntTag))]
    private Task<bool> AddIntTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_INT); });
    }

    [RelayCommand(CanExecute = nameof(CanAddLongTag))]
    private Task<bool> AddLongTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_LONG); });
    }

    [RelayCommand(CanExecute = nameof(CanAddFloatTag))]
    private Task<bool> AddFloatTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_FLOAT); });
    }

    [RelayCommand(CanExecute = nameof(CanAddDoubleTag))]
    private Task<bool> AddDoubleTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_DOUBLE); });
    }

    [RelayCommand(CanExecute = nameof(CanAddByteArrayTag))]
    private Task<bool> AddByteArrayTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_BYTE_ARRAY); });
    }

    [RelayCommand(CanExecute = nameof(CanAddIntArrayTag))]
    private Task<bool> AddIntArrayTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_INT_ARRAY); });
    }

    [RelayCommand(CanExecute = nameof(CanAddLongArrayTag))]
    private Task<bool> AddLongArrayTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_LONG_ARRAY); });
    }

    [RelayCommand(CanExecute = nameof(CanAddStringTag))]
    private Task<bool> AddStringTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_STRING); });
    }

    [RelayCommand(CanExecute = nameof(CanAddListTag))]
    private Task<bool> AddListTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_LIST); });
    }

    [RelayCommand(CanExecute = nameof(CanAddCompoundTag))]
    private Task<bool> AddCompoundTag()
    {
        return SafeExecuteAsync(async () => { await AddTag(TagType.TAG_COMPOUND); });
    }

    // This one is executed when the user chooses to Expand or Collapse a TreeNode.
    [RelayCommand(CanExecute = nameof(CanToggleExpand))]
    private Task<bool> ToggleExpand()
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
    private Task<bool> ExpandChildren()
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
    private Task<bool> ExpandTree()
    {
        return SafeExecuteAsync(async () =>
        {
            // Check if selectedTreeNode is null.
            if (SingleSelectedTreeNode is null) throw new UnreachableException();

            await WithBlock(async () => await SingleSelectedTreeNode.ExpandTreeAsync(), true);
        });
    }

    // This allows us to easily catch any errors!
    internal async Task<bool> SafeExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception e)
        {
            // If the exception comes from Substrate, things are probably on fire. That's fatal.
            var fatal = e is SubstrateException;

            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Write(fatal ? LogEventLevel.Fatal : LogEventLevel.Error, e,
                "[NBTLoupe]: RelayCommand exception");

            await OpenDialogAsync(new ErrorDialogViewModel(this, e, fatal));

            return false;
        }
        finally
        {
            // Oh, and it also allows us to easily tell the Save Button to update!
            SaveCommand.NotifyCanExecuteChanged();
        }
    }
}
