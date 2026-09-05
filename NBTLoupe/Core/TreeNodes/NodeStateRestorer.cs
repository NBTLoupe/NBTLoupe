using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NBTLoupe.Core.TreeNodes;

internal static class NodeStateRestorer
{
    extension(TreeNode node)
    {
        // When a Refresh occurs, the IsExpanded (UI-wise) TreeNodes are lost. This function backs them all up...
        internal HashSet<string> SaveExpandedNodes(bool checkParents = true)
        {
            // ...by creating a HashSet...
            var expandedNodes = new HashSet<string>();

            // Immediately return if it doesn't have SubNodes.
            if (node.SubNodes is null) return expandedNodes;

            if (checkParents)
            {
                // We save the current node...
                var current = node;

                // ...and while the current TreeNode is not null (AKA we haven't reached the root yet)...
                while (current is not null)
                {
                    // ...if it IsExpanded (which it should), we add it to our HashSet...
                    if (current.IsExpanded) expandedNodes.Add(current.DataNode.NodePath);

                    // ...and we keep climbing to the next parent.
                    current = current.Parent;
                }
            }

            foreach (var child in node.SubNodes)
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
        internal async Task RestoreExpandedNodesAsync(HashSet<string> expandedNodes)
        {
            // Immediately return if it doesn't have SubNodes.
            if (node.SubNodes is null) return;

            // ...by looping through all the TreeNode children...
            foreach (var child in node.SubNodes)
            {
                // ...making sure they're lazy-loaded...
                await child.LazyLoadAsync();

                // ...and only IsExpanded them if they're in the HashNet. 
                if (!expandedNodes.Contains(child.DataNode.NodePath)) continue;
                child.IsExpanded = true;

                // If that's the case, we loop and continue checking and IsExpanding their children.
                await child.RestoreExpandedNodesAsync(expandedNodes);
            }
        }

        // After certain operations, the SelectedTreeNode is invalidated. This functions backs its index up...
        internal List<int> GetIndexPath(ObservableCollection<TreeNode> treeNodes)
        {
            // ...by creating a List...
            var indexPath = new List<int>();

            // ...storing the current TreeNode...
            var current = node;

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
}
