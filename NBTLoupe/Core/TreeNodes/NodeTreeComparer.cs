using System.Collections.Generic;
using NBTModel.Data.Nodes;
using NBTModel.Utility;
using Substrate.Nbt;

namespace NBTLoupe.Core.TreeNodes;

// More reused infrastructure! This sorts the TreeNode in exactly the same way as the original NBTExplorer!
internal class NodeTreeComparer : IComparer<DataNode>
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
