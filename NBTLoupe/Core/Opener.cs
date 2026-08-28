using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using NBTLoupe.ViewModels.Dialogs;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data;
using NBTModel.Data.Nodes;

namespace NBTLoupe.Core;

internal static class Opener
{
    // This function Opens a File from a Path.
    internal static async Task OpenFileAsync(MainViewModel viewModel, string path)
    {
        // If the user has unsaved changes...
        var shouldContinue = !viewModel.CanSave;

        // ...we open a Dialog to warn them.
        if (!shouldContinue)
            shouldContinue = await viewModel.OpenDialogAsync(new UnsavedChangesDialogViewModel(viewModel));

        // And if the user Cancelled, we return.
        if (!shouldContinue) return;

        await viewModel.WithBlock(async () =>
        {
            // First we clear the TreeNode collections, as we're starting fresh.
            viewModel.SelectedTreeNodes.Clear();
            viewModel.TreeNodes.Clear();

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
            viewModel.RecentFiles.Clear();
            foreach (var item in RecentItem.Add(path, false).Where(x => !x.IsFolder)) viewModel.RecentFiles.Add(item);

            // And we can begin the lazy-loading!
            await Dispatcher.UIThread.InvokeAsync(
                () => TreeNode.ExpandNode([node], viewModel.TreeNodes, viewModel.WithBlock, viewModel.OpenDialogAsync),
                DispatcherPriority.Background);
        }, false, true);
    }

    // This function Opens a Folder from a Path.
    internal static async Task OpenFolderAsync(MainViewModel viewModel, string path)
    {
        // If the user has unsaved changes...
        var shouldContinue = !viewModel.CanSave;

        // ...we open a Dialog to warn them.
        if (!shouldContinue)
            shouldContinue = await viewModel.OpenDialogAsync(new UnsavedChangesDialogViewModel(viewModel));

        // And if the user Cancelled, we return.
        if (!shouldContinue) return;

        await viewModel.WithBlock(async () =>
        {
            // First we clear the TreeNode collections, as we're starting fresh.
            viewModel.SelectedTreeNodes.Clear();
            viewModel.TreeNodes.Clear();

            // If it isn't the Minecraft Saves folder; we add it to our Recent Folders list, and update the UI!
            if (path != Program.MinecraftSaveFolder)
            {
                viewModel.RecentFolders.Clear();
                foreach (var item in RecentItem.Add(path, true).Where(x => x.IsFolder))
                    viewModel.RecentFolders.Add(item);
            }

            // And we can begin the lazy-loading!
            await Dispatcher.UIThread.InvokeAsync(
                () => TreeNode.ExpandNode([new DirectoryDataNode(path.TrimEnd('/', '\\'))], viewModel.TreeNodes,
                    viewModel.WithBlock, viewModel.OpenDialogAsync),
                DispatcherPriority.Background);
        }, false, true);
    }
}
