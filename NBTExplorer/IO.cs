using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NBTExplorer.Model;
using Serilog;

namespace NBTExplorer;

// We need this so NAOT is happy.
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(List<RecentItem>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

// And this is the class which defines our RecentItems!
internal class RecentItem
{
    // Here's the Path of the RecentItem.
    public string Path { get; init; } = "";

    // And this is true if the Item is a Directory/Folder.
    public bool IsFolder { get; init; }

    // This is our fancy DisplayPath so we don't fill the RecentItems menu.
    internal string DisplayPath
    {
        get
        {
            // We replace backslashes with normal slashes to keep Windows compatibility.
            var antiWindowsPath = Path.Replace('\\', '/');
            
            // If it isn't stored in the Minecraft world saves folder...
            var savesIndex = antiWindowsPath.IndexOf("/saves/", StringComparison.InvariantCultureIgnoreCase);
            if (savesIndex < 0)
            {
                // ...we just cut to the parent directory.
                var fileInfo = new FileInfo(antiWindowsPath);
                return $".../{fileInfo.Directory?.Name}/{fileInfo.Name}";
            }

            // But if it is stored in the Minecraft world saves folder...

            // We cut the path up to the name.
            var relativePath = antiWindowsPath[(savesIndex + 7)..];

            // Then we find the first slash after the world name.
            var firstSlash = relativePath.IndexOfAny(['/', '\\']);

            // If for some reason we didn't, we likely failed to find a true world.
            if (firstSlash < 0)
            {
                var fileInfo = new FileInfo(antiWindowsPath);
                return $".../{fileInfo.Directory?.Name}/{fileInfo.Name}";
            }

            // But if we did, we can isolate the world name from the rest of the path, making it clear what it is!
            var worldName = relativePath[..firstSlash];
            var restOfPath = relativePath[(firstSlash + 1)..];
            return $"[{worldName}]/{restOfPath}";
        }
    }

    // And this method helps us Load our data file!
    internal static List<RecentItem> Load(bool startingUp = false)
    {
        // First, we check if it exists. If it doesn't, we start fresh!
        if (!File.Exists(Program.RecentItemsFile)) return [];

        try
        {
            // First we read our data file.
            var json = File.ReadAllText(Program.RecentItemsFile);

            // Then we deserialize it.
            var items = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ListRecentItem) ?? [];

            // If we're not starting up the app, we just return that data...
            if (!startingUp) return items;

            // ...but if we are, we quickly check all the RecentItems still exist.
            var existing = items.Where(item => File.Exists(item.Path) || Directory.Exists(item.Path)).ToList();

            // If they do, we just return these!
            if (existing.Count == items.Count) return items;

            // If they don't, we clean up our data file and only return the ones that do exist!
            File.WriteAllText(Program.RecentItemsFile,
                JsonSerializer.Serialize(existing, SourceGenerationContext.Default.ListRecentItem));
            return existing;
        }
        catch
        {
            // We also start fresh if it's corrupted for some reason.
            return [];
        }
    }

    // And this method helps us Add RecentItems to our data file!
    internal static List<RecentItem> Add(string path, bool isFolder)
    {
        // First we Load a fresh copy of our data file.
        var preItems = Load();

        // If the RecentItem to add is already in there, we get rid of it...
        preItems.RemoveAll(x => x.Path == path);

        // ...so we can add it (back) at the top!
        preItems.Insert(0, new RecentItem
        {
            Path = path,
            IsFolder = isFolder
        });

        // Then we make sure our RecentItems only have 10 per-kind. We don't want to make our list overwhelming!
        var items = preItems.GroupBy(x => x.IsFolder).SelectMany(g => g.Take(10)).ToList();

        try
        {
            // And now we can save our updated data file!
            File.WriteAllText(Program.RecentItemsFile,
                JsonSerializer.Serialize(items, SourceGenerationContext.Default.ListRecentItem));
        }
        catch (Exception e)
        {
            // Or we can fail at it...
            Log.Error(e, "[neoNBTExplorer]: RecentItems (adding) exception");
        }

        return items;
    }

    // And this method helps us Remove RecentItems to our data file!
    internal static void Remove(string path)
    {
        // First we Load a fresh copy of our data file.
        var items = Load();

        // If the RecentItem isn't in the data file already, we just return...
        if (items.RemoveAll(x => x.Path == path) < 1) return;

        try
        {
            // ...but if it's there, we need to update our data file!
            File.WriteAllText(Program.RecentItemsFile,
                JsonSerializer.Serialize(items, SourceGenerationContext.Default.ListRecentItem));
        }
        catch (Exception e)
        {
            // Or we can fail at it...
            Log.Error(e, "[neoNBTExplorer]: RecentItems (removing) exception");
        }
    }
}

public partial class MainWindow
{
    // We need a RecentItem implementation to be able to interface with the UI!
    internal ObservableCollection<RecentItem> RecentFiles { get; set; }
    internal ObservableCollection<RecentItem> RecentFolders { get; set; }

    // This function Opens a File from a Path.
    private async Task OpenFileAsync(string path)
    {
        IsBlocked = true;

        try
        {
            // First we clear the TreeNode collections, as we're starting fresh.
            SelectedTreeNodes.Clear();
            TreeNodes.Clear();
            
            // We disable the Save button, as the postExecute task may not be instant for this specific case.
            Save.Toggle(false);

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
            RecentFiles.Clear();
            foreach (var item in RecentItem.Add(path, false).Where(x => !x.IsFolder))
            {
                RecentFiles.Add(item);
            }

            // And we can start expanding our NodeTree!
            await TreeNode.ExpandNodeAsync([node], TreeNodes);
        }
        finally
        {
            IsBlocked = false;
        }
    }

    // This function Opens a Folder from a Path.
    private async Task OpenFolderAsync(string path)
    {
        IsBlocked = true;

        try
        {
            // First we clear the TreeNode collections, as we're starting fresh.
            SelectedTreeNodes.Clear();
            TreeNodes.Clear();
            
            // We disable the Save button, as the postExecute task may not be instant for this specific case.
            Save.Toggle(false);

            // We add it to our Recent Folders list, and update the UI!
            RecentFolders.Clear();
            foreach (var item in RecentItem.Add(path, true).Where(x => x.IsFolder))
            {
                RecentFolders.Add(item);
            }

            // And we can start expanding our NodeTree! Trimming the trailing slashes as NBTModel doesn't like them. 
            await TreeNode.ExpandNodeAsync([new DirectoryDataNode(path.TrimEnd('/', '\\'))], TreeNodes);
        }
        finally
        {
            IsBlocked = false;
        }
    }
}