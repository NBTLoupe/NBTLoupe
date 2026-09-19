using System;
using NBTModel.Data.Nodes;

namespace NBTLoupe.Core.TreeNodes;

// This does the *actual* search work of the Basic Find.
internal class NodeSearcherBasic(TreeNode parent, string? name, string? value) : NodeSearcher(parent)
{
    // This checks both of our Basic Search conditions with a given TreeNode.
    protected override bool IsMatch(TreeNode node)
    {
        return node.DataNode is TagDataNode &&
               (name is null ||
                node.DataNode.NodeName.Contains(name,
                    StringComparison.InvariantCultureIgnoreCase)) &&
               (value is null ||
                node.DataNode.NodeDisplay.Contains(value,
                    StringComparison.InvariantCultureIgnoreCase));
    }
}
