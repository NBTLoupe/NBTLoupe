using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using NBTLoupe.Core.TreeNodes;
using NBTLoupe.ViewModels.Dialogs;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data;
using NBTModel.Data.Nodes;

namespace NBTLoupe.Core.IO;

internal static class Opener
{
    // This function Opens a File from a Path.
    internal static async Task OpenFileAsync(MainViewModel mainViewModel, string path)
    {
        // If the user has unsaved changes...
        var shouldContinue = !mainViewModel.CanSave;

        // ...we open a Dialog to warn them.
        if (!shouldContinue)
            shouldContinue = await mainViewModel.OpenDialogAsync(new UnsavedChangesDialogViewModel(mainViewModel));

        // And if the user Cancelled, we return.
        if (!shouldContinue) return;

        await mainViewModel.WithBlock(async () =>
        {
            // First we clear the TreeNode collections, as we're starting fresh.
            mainViewModel.SelectedTreeNodes.Clear();
            mainViewModel.TreeNodes.Clear();

            // We check, from the Path, if the File is supported by NBTModel, and use its respective NodeCreate method to create our DataNode if so.
            var node = FileTypeRegistry.RegisteredTypes.FirstOrDefault(item => item.Value.NamePatternTest(path)).Value
                ?.NodeCreate(path);

            // If we couldn't find any Path-based matches, we just assume it is a NbtFileDataNode...
            node ??= NbtFileDataNode.TryCreateFrom(path);

            // And if it failed to open, we tell the user.
            if (node is null)
                throw new UserErrorException(
                    "Invalid NBT file. Please only open supported file formats. If you did so, your file may be corrupted.");

            // We add it to our Recent Files list, and update the UI!
            mainViewModel.RecentFiles.Clear();
            foreach (var item in RecentItem.Add(path, false).Where(x => !x.IsFolder))
                mainViewModel.RecentFiles.Add(item);

            // And we can begin the lazy-loading!
            await Dispatcher.UIThread.InvokeAsync(
                () => TreeNode.ExpandNode([node], mainViewModel.TreeNodes, mainViewModel.WithBlock,
                    factory => mainViewModel.OpenDialogAsync(factory(mainViewModel))),
                DispatcherPriority.Background);
        }, false, true);
    }

    // This function Opens a Folder from a Path.
    internal static async Task OpenFolderAsync(MainViewModel mainViewModel, string path)
    {
        // If the user has unsaved changes...
        var shouldContinue = !mainViewModel.CanSave;

        // ...we open a Dialog to warn them.
        if (!shouldContinue)
            shouldContinue = await mainViewModel.OpenDialogAsync(new UnsavedChangesDialogViewModel(mainViewModel));

        // And if the user Cancelled, we return.
        if (!shouldContinue) return;

        await mainViewModel.WithBlock(async () =>
        {
            // First we clear the TreeNode collections, as we're starting fresh.
            mainViewModel.SelectedTreeNodes.Clear();
            mainViewModel.TreeNodes.Clear();

            // If it isn't the Minecraft Saves folder; we add it to our Recent Folders list, and update the UI!
            if (path != Program.MinecraftSaveFolder)
            {
                mainViewModel.RecentFolders.Clear();
                foreach (var item in RecentItem.Add(path, true).Where(x => x.IsFolder))
                    mainViewModel.RecentFolders.Add(item);
            }

            // And we can begin the lazy-loading!
            await Dispatcher.UIThread.InvokeAsync(
                () => TreeNode.ExpandNode([new DirectoryDataNode(path.TrimEnd('/', '\\'))], mainViewModel.TreeNodes,
                    mainViewModel.WithBlock, factory => mainViewModel.OpenDialogAsync(factory(mainViewModel))),
                DispatcherPriority.Background);
        }, false, true);
    }
}
