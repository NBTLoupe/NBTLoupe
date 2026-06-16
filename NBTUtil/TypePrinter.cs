using System;
using System.Collections.Generic;
using NBTModel.Data.Nodes;

namespace NBTUtil;

internal static class TypePrinter
{
    private static readonly Dictionary<Type, string> Key = new()
    {
        { typeof(TagByteDataNode), "b" },
        { typeof(TagShortDataNode), "s" },
        { typeof(TagIntDataNode), "i" },
        { typeof(TagLongDataNode), "l" },
        { typeof(TagFloatDataNode), "f" },
        { typeof(TagDoubleDataNode), "d" },
        { typeof(TagStringDataNode), "T" },
        { typeof(TagByteArrayDataNode), "B" },
        { typeof(TagIntArrayDataNode), "I" },
        { typeof(TagShortArrayDataNode), "S" },
        { typeof(TagLongArrayDataNode), "L" },
        { typeof(TagListDataNode), "L" },
        { typeof(TagCompoundDataNode), "C" },
        { typeof(NbtFileDataNode), "N" },
        { typeof(RegionFileDataNode), "R" },
        { typeof(RegionChunkDataNode), "r" },
        { typeof(CubicRegionDataNode), "R" },
        { typeof(DirectoryDataNode), "/" }
    };

    public static string Print(DataNode node, bool showType)
    {
        if (!Key.ContainsKey(node.GetType()))
            return "";

        if (showType)
            return "<" + Key[node.GetType()] + "> " + node.NodeDisplay;
        return node.NodeDisplay;
    }
}