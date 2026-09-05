using System.Threading.Tasks;
using NBTModel.Data.Nodes;

namespace NBTLoupe.Core.TreeNodes;

internal static class NodeRegionSearcher
{
    // This function allows us to Search for a specific Regional TreeNode.
    internal static async Task<TreeNode?> SearchRegionAsync(this TreeNode node, int regionX, int regionZ,
        int localChunkX, int localChunkZ)
    {
        // Starting at our root DataNode...
        switch (node.DataNode)
        {
            // ...if it's a Directory...
            case DirectoryDataNode:
            {
                switch (node.SubNodes)
                {
                    // Immediately return if it doesn't have SubNodes.
                    case null:
                        return null;

                    // And, if we didn't yet, we lazy-load it.
                    case [{ IsPlaceholder: true }]:
                        await node.LazyLoadAsync();
                        break;
                }

                // Afterwards, we loop through its children...
                foreach (var subNode in node.SubNodes)
                {
                    // ...to keep searching on them...
                    var resultNode = await subNode.SearchRegionAsync(regionX, regionZ, localChunkX, localChunkZ);

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
                switch (node.SubNodes)
                {
                    // Immediately return if it doesn't have SubNodes.
                    case null:
                        return null;

                    // And, if we didn't yet, we lazy-load it.
                    case [{ IsPlaceholder: true }]:
                        await node.LazyLoadAsync();
                        break;
                }

                // Afterwards, we loop through its children...
                foreach (var subNode in node.SubNodes)
                {
                    // ...to keep searching on them...
                    var resultNode = await subNode.SearchRegionAsync(regionX, regionZ, localChunkX, localChunkZ);

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
                return node;
        }

        // And if we didn't find anything, we just return that.
        return null;
    }
}
