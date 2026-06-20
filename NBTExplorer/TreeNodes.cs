using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using NBTModel.Data.Nodes;
using NBTModel.Utility;
using Substrate.Nbt;

namespace NBTExplorer;

public partial class MainWindow
{
    // We need a TreeNode implementation to be able to interface with Avalonia's TreeView...
    internal ObservableCollection<TreeNode> TreeNodes { get; }
    internal ObservableCollection<TreeNode> SelectedTreeNodes { get; }

    internal static string GetFriendlyTag(TagType? tagType)
    {
        return tagType switch
        {
            TagType.TAG_BYTE => "Byte Tag",
            TagType.TAG_SHORT => "Short Tag",
            TagType.TAG_INT => "Int Tag",
            TagType.TAG_LONG => "Long Tag",
            TagType.TAG_FLOAT => "Float Tag",
            TagType.TAG_DOUBLE => "Double Tag",

            TagType.TAG_BYTE_ARRAY => "Byte Array Tag",
            TagType.TAG_SHORT_ARRAY => "Short Array Tag",
            TagType.TAG_INT_ARRAY => "Int Array Tag",
            TagType.TAG_LONG_ARRAY => "Long Array Tag",

            TagType.TAG_STRING => "String Tag",
            TagType.TAG_LIST => "List Tag",

            TagType.TAG_COMPOUND => "Compound Tag",

            _ => tagType is not null ? tagType.ToString() : "Tag"
        } ?? "Tag";
    }

    // ...which is defined here and...
    internal class TreeNode : INotifyPropertyChanged
    {
        // More reused infrastructure! This sorts the TreeNode in exactly the same way as the original NBTExplorer!
        private static readonly NodeTreeComparer NodeComparer = new();

        // And here's how you create the actual TreeNode!
        private TreeNode(DataNode dataNode, ObservableCollection<TreeNode> subNodes, bool isPlaceholder = false)
        {
            DataNode = dataNode;
            SubNodes = subNodes;
            IsPlaceholder = isPlaceholder;
        }

        // ...it includes its children (SubNodes), its data (DataNode), and its Parent.
        internal ObservableCollection<TreeNode>? SubNodes { get; }
        internal DataNode DataNode { get; }
        internal TreeNode? Parent { get; private set; }
        internal bool IsPlaceholder { get; }

        // Oh, but all that data is for our fun. Avalonia cares about its Title and its Icon, which is here.
        internal string Title => DataNode.NodeDisplay;

        internal string Icon => DataNode switch
        {
            // Here's the list that matches a FluentIcon to each DataNode Type!
            TagByteDataNode => "NumberCircle1",
            TagShortDataNode => "NumberCircle2",
            TagIntDataNode => "NumberCircle4",
            TagLongDataNode => "NumberCircle8",
            TagFloatDataNode => "DecimalArrowLeft",
            TagDoubleDataNode => "DecimalArrowRight",

            TagByteArrayDataNode => "CodeBlock",
            TagIntArrayDataNode => "DataBarVertical",
            TagLongArrayDataNode => "DataBarHorizontal",

            TagStringDataNode => "TextT",
            TagListDataNode => "TextBulletList",

            TagCompoundDataNode => "Box",

            DirectoryDataNode => "Folder",
            NbtFileDataNode => "Archive",
            RegionChunkDataNode => "Archive",

            RegionFileDataNode => "Cube",
            CubicRegionDataNode => "Cube",

            _ => "QuestionCircle"
        };

        // Create an IsExpanded property, that fires the handler above when modified.
        internal bool IsExpanded
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        // Add an event handler that fires if the state changed.
        public event PropertyChangedEventHandler? PropertyChanged;

        // Oh, and this is how you refresh its Title if you have to.
        internal void RefreshTitle()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }

        // Oh, and here's how you set its parent!
        private void SetParent(TreeNode? parent)
        {
            Parent = parent;
        }

        // Nodes in NBTModel have to be "Expanded" to be able to access their children. This does that in a sorted manner.
        internal static async Task ExpandNodeAsync(IList<DataNode> nodeTree, ObservableCollection<TreeNode> treeNodes,
            TreeNode? parent = null)
        {
            // First we sort the NodeTree...
            var sortedNodeTree = nodeTree.OrderBy(dataNode => dataNode, NodeComparer);

            foreach (var dataNode in sortedNodeTree)
            {
                // ...then call the previously mentioned Expand method in each of its children.
                dataNode.Expand();

                // Once that's done, we can create its respective SubNodes collection...
                var subNodes = new ObservableCollection<TreeNode>();

                // ...and initialize a new TreeNode with it.
                var treeNode = new TreeNode(dataNode, subNodes);
                treeNode.SetParent(parent);

                // And finally, we can add the Expanded one back to its parent.
                await Dispatcher.UIThread.InvokeAsync(() => treeNodes.Add(treeNode),
                    DispatcherPriority.Background);

                // We do need to add a Placeholder so the arrow shows, though!
                if (dataNode.Nodes.Count < 1) continue;

                var placeholder = new TreeNode(new TagStringDataNode(""), [], true);
                placeholder.SetParent(parent);
                await Dispatcher.UIThread.InvokeAsync(() => subNodes.Add(placeholder), DispatcherPriority.Background);
            }
        }

        // This function helps with lazy-loading, mainly ensuring the Placeholder gets deleted.
        internal async Task LazyLoadAsync(IList<DataNode>? dataNode = null)
        {
            if (SubNodes is not [{ IsPlaceholder: true }]) return;

            // We Expand its real children lazily, and Stage them...
            var staged = new ObservableCollection<TreeNode>();
            await ExpandNodeAsync(dataNode ?? DataNode.Nodes, staged, this);

            // ...so we can replace our stubby/lazy SubNodes with the Staged ones.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SubNodes.Clear();
                foreach (var node in staged) SubNodes.Add(node);
            }, DispatcherPriority.Background);
        }

        // This IsExpands (UI-wise) an entire TreeNode. Related to the ExpandTree AppCommand. 
        internal async Task ExpandTreeAsync()
        {
            switch (SubNodes)
            {
                // We immediately return if it doesn't have SubNodes...
                case null:
                    return;
                // ...then we lazy-load its children...
                case [{ IsPlaceholder: true }]:
                    await LazyLoadAsync();
                    break;
            }

            // ... then we expand the TreeNode itself...
            await Dispatcher.UIThread.InvokeAsync(() => IsExpanded = true, DispatcherPriority.Background);

            // ...and we loop until the entire TreeNode IsExpanded (UI-wise).
            foreach (var child in SubNodes.ToList()) await child.ExpandTreeAsync();
        }

        // This IsExpands (UI-wise) an entire TreeNode, but the other way around. 
        internal async Task ExpandTreeReverseAsync()
        {
            // If it has a Parent, we loop until the entire TreeNode IsExpanded (UI-wise).
            if (Parent is not null) await Parent.ExpandTreeReverseAsync();

            // And this is how we expand the TreeNode itself.
            await Dispatcher.UIThread.InvokeAsync(() => IsExpanded = true, DispatcherPriority.Background);
        }

        // This refreshes a TreeNode. Required to display in the UI any change in it.
        internal async Task RefreshChildNodesAsync()
        {
            // Immediately return if it doesn't have SubNodes.
            if (SubNodes is null) return;

            // First we back up the current SubNodes...
            var currentNodes = SubNodes.ToDictionary(treeNode => treeNode.DataNode, treeNode => treeNode);
            // ...as we're going to clear the original's.
            await Dispatcher.UIThread.InvokeAsync(() => SubNodes.Clear(), DispatcherPriority.Background);

            // Then we sort the NodeTree...
            var sortedNodeTree = DataNode.Nodes.OrderBy(dataNode => dataNode, NodeComparer);

            foreach (var child in sortedNodeTree)
                // Then for each already-Expanded child (from the currentNodes)...
                if (currentNodes.TryGetValue(child, out var existing))
                {
                    // ...we readd it to the SubNodes, and Refresh it if needed.
                    existing.SetParent(this);
                    if (!child.HasUnexpandedChildren) await existing.RefreshChildNodesAsync();
                    await Dispatcher.UIThread.InvokeAsync(() => SubNodes.Add(existing), DispatcherPriority.Background);
                }

            if (SubNodes.Count < 1)
            {
                // If we didn't add any children, we prepare the parent for lazy-loading.
                var placeholder = new TreeNode(new TagStringDataNode(""), [], true);
                placeholder.SetParent(this);
                await Dispatcher.UIThread.InvokeAsync(() => SubNodes.Add(placeholder), DispatcherPriority.Background);
            }

            // Then we refresh its Title. Usually not necessary, but useful when the root is being Refreshed.
            RefreshTitle();
        }

        // When a Refresh occurs, the IsExpanded (UI-wise) TreeNodes are lost. This function backs them all up...
        internal HashSet<string> SaveExpandedNodes(bool checkParents = true)
        {
            // ...by creating a HashSet...
            var expandedNodes = new HashSet<string>();

            // Immediately return if it doesn't have SubNodes.
            if (SubNodes is null) return expandedNodes;

            if (checkParents)
            {
                // We save the current node...
                var current = this;

                // ...and while the current TreeNode is not null (AKA we haven't reached the root yet)...
                while (current is not null)
                {
                    // ...if it IsExpanded (which it should), we add it to our HashSet...
                    if (!current.IsExpanded) continue;
                    expandedNodes.Add(current.DataNode.NodePath);

                    // ...and we keep climbing to the next parent.
                    current = current.Parent;
                }
            }

            foreach (var child in SubNodes)
            {
                // ...that only contains IsExpanded TreeNodes...
                if (!child.IsExpanded) continue;
                expandedNodes.Add(child.DataNode.NodePath);

                // ...and their children, for which we loop.
                expandedNodes.UnionWith(child.SaveExpandedNodes(false));
            }

            // And finally, we return the completed backup.
            return expandedNodes;
        }

        // This function restores the backup created by the previous function...
        internal async Task RestoreExpandedNodes(HashSet<string> expandedNodes)
        {
            // Immediately return if it doesn't have SubNodes.
            if (SubNodes is null) return;

            // ...by looping through all the TreeNode children...
            foreach (var child in SubNodes)
            {
                // ...making sure they're lazy-loaded...
                await child.LazyLoadAsync();

                // ...and only IsExpanded them if they're in the HashNet. 
                if (!expandedNodes.Contains(child.DataNode.NodePath)) continue;
                child.IsExpanded = true;

                // If that's the case, we loop and continue checking and IsExpanding their children.
                await child.RestoreExpandedNodes(expandedNodes);
            }
        }

        // After certain operations, the SelectedTreeNode is invalidated. This functions backs its index up...
        internal List<int> GetIndexPath(ObservableCollection<TreeNode> treeNodes)
        {
            // ...by creating a List...
            var indexPath = new List<int>();

            // ...storing the current TreeNode...
            var current = this;

            // ...and while the current TreeNode is not null (AKA we haven't reached the root yet)...
            while (current is not null)
            {
                // ...we get its index...
                var index = (current.Parent?.SubNodes ?? treeNodes).IndexOf(current);

                // ...if it exists, we add it to our indexPath List...
                if (index < 0) return [];
                indexPath.Insert(0, index);

                // ...and we keep climbing to the next parent.
                current = current.Parent;
            }

            // Once we reached the root, we return our finalised indexPath backup.
            return indexPath;
        }

        // This function restores the backup created by the previous function...
        internal static TreeNode? GetByIndexPath(ObservableCollection<TreeNode> treeNodes, List<int> indexPath)
        {
            // Immediately return if it doesn't have any indexes.
            if (indexPath.Count < 1) return null;

            // ...we get our first child...
            var current = treeNodes.ElementAtOrDefault(indexPath[0]);

            // ...then for every other index...
            foreach (var index in indexPath.Skip(1))
            {
                // ...if the index is invalid, we return null...
                if (current?.SubNodes is null) return null;

                // ...otherwise, we save that child and keep climbing. 
                var next = current.SubNodes.ElementAtOrDefault(index);
                if (next is not null)
                {
                    current = next;
                    continue;
                }

                // In case we couldn't find it, we default to the previous one. And in case we couldn't find that one, we default to the last child.
                current = current.SubNodes.ElementAtOrDefault(index - 1) ?? current.SubNodes.LastOrDefault();
                break;
            }

            // Once we finish climbing, we return the best TreeNode we found.
            return current;
        }

        // This function allows us to Search for a specific child TreeNode.
        internal async Task<TreeNode?> SearchAsync(int regionX, int regionZ, int localChunkX, int localChunkZ)
        {
            // Starting at our root DataNode...
            switch (DataNode)
            {
                // ...if it's a Directory...
                case DirectoryDataNode:
                {
                    switch (SubNodes)
                    {
                        // Immediately return if it doesn't have SubNodes.
                        case null:
                            return null;

                        // And, if we didn't yet, we lazy-load it.
                        case [{ IsPlaceholder: true }]:
                            await LazyLoadAsync();
                            break;
                    }

                    // Afterwards, we loop through its children...
                    foreach (var subNode in SubNodes)
                    {
                        // ...to keep searching on them...
                        var resultNode = await subNode.SearchAsync(regionX, regionZ, localChunkX, localChunkZ);

                        // ...until one of them is the result.
                        if (resultNode != null) return resultNode;
                    }

                    break;
                }

                // ...if it's a RegionFile...
                case RegionFileDataNode regionNode:
                {
                    // ...if we aren't in the right Region, we immediately return.
                    if (!RegionFileDataNode.RegionCoordinates(regionNode.NodePathName, out var rx, out var rz))
                        return null;
                    if (rx != regionX || rz != regionZ)
                        return null;

                    // But if it is the right Region...
                    switch (SubNodes)
                    {
                        // Immediately return if it doesn't have SubNodes.
                        case null:
                            return null;

                        // And, if we didn't yet, we lazy-load it.
                        case [{ IsPlaceholder: true }]:
                            await LazyLoadAsync();
                            break;
                    }

                    // Afterwards, we loop through its children...
                    foreach (var subNode in SubNodes)
                    {
                        // ...to keep searching on them...
                        var resultNode = await subNode.SearchAsync(regionX, regionZ, localChunkX, localChunkZ);

                        // ...until one of them is the result.
                        if (resultNode != null) return resultNode;
                    }

                    break;
                }

                // ...if it's a Chunk...
                // ...it either isn't the right one...
                case RegionChunkDataNode chunkNode when chunkNode.X != localChunkX || chunkNode.Z != localChunkZ:
                    break;

                // ...or we found it! In which case, we return it.
                case RegionChunkDataNode:
                    return this;
            }

            // And if we didn't find anything, we just return that.
            return null;
        }

        private class NodeTreeComparer : IComparer<DataNode>
        {
            private readonly NaturalComparer _comparer = new();

            // Then the actual comparing occurs!
            public int Compare(DataNode? x, DataNode? y)
            {
                // Immediately return if the DataNodes are null
                if (x is null || y is null) return 0;

                // We get the TagDataNode of each DataNode to compare...
                var tagDataNodeX = x as TagDataNode;
                var tagDataNodeY = y as TagDataNode;

                // ...then we get its Tag.
                var tagNodeX = tagDataNodeX?.Tag;
                var tagNodeY = tagDataNodeY?.Tag;

                // If it doesn't have a Tag...
                if (tagNodeX is null || tagNodeY is null)
                {
                    // ...we OrderForNode.
                    var nodeOrder = OrderForNode(x).CompareTo(OrderForNode(y));

                    // But if that didn't help, we resort to their NodeDisplay.
                    return nodeOrder != 0 ? nodeOrder : _comparer.Compare(x.NodeDisplay, y.NodeDisplay);
                }

                // We get their Parents as TagDataNodes...
                if (tagDataNodeX?.Parent is TagDataNode parentX && tagDataNodeY?.Parent is TagDataNode parentY)
                    // ...and prioritize them if they're TAG_LISTs.
                    if (parentX.Tag.GetTagType() == TagType.TAG_LIST || parentY.Tag.GetTagType() == TagType.TAG_LIST)
                        return 0;

                // Then finally, we get their TagTypes...
                var tagTypeX = tagNodeX.GetTagType();
                var tagTypeY = tagNodeY.GetTagType();

                // ...to be able to OrderForTag.
                var tagOrder = OrderForTag(tagTypeX).CompareTo(OrderForTag(tagTypeY));

                // But if that didn't help, we resort to their NodeDisplay.
                return tagOrder != 0
                    ? tagOrder
                    : _comparer.Compare(tagDataNodeX?.NodeDisplay, tagDataNodeY?.NodeDisplay);
            }

            // Each Tag has different order priorities, these are set here.
            private static int OrderForTag(TagType tagId)
            {
                return tagId switch
                {
                    TagType.TAG_COMPOUND => 0,
                    TagType.TAG_LIST => 1,
                    TagType.TAG_BYTE or TagType.TAG_SHORT or TagType.TAG_INT or TagType.TAG_LONG or TagType.TAG_FLOAT
                        or TagType.TAG_DOUBLE or TagType.TAG_STRING => 2,
                    _ => 3
                };
            }

            // And DirectoryDataNodes also do, so that is set here.
            private static int OrderForNode(DataNode node)
            {
                return node is DirectoryDataNode ? 0 : 1;
            }
        }
    }
}
