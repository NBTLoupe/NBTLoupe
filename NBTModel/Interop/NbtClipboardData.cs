using System;
using System.IO;
using Substrate.Nbt;

namespace NBTModel.Interop;

public class NbtClipboardData(string name, TagNode node)
{
    public string Name { get; } = name;

    public TagNode Node { get; } = node;

    public static byte[] SerializeNode(TagNode node)
    {
        var root = new TagNodeCompound { { "root", node } };
        var tree = new NbtTree(root);

        using var ms = new MemoryStream();
        tree.WriteTo(ms);
        var data = new byte[ms.Length];
        Array.Copy(ms.GetBuffer(), data, ms.Length);

        return data;
    }

    public static TagNode? DeserializeNode(byte[] data)
    {
        var tree = new NbtTree();
        using (var ms = new MemoryStream(data))
        {
            tree.ReadFrom(ms);
        }

        var root = tree.Root;
        if (root == null || !root.TryGetValue("root", out var node))
            return null;

        return node;
    }
}