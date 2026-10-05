/*
 *  Copyright (c) 2024-2026 MallardLuna
 *  
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;

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
