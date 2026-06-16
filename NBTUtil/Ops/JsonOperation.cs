using System.IO;
using NBTModel.Data.Nodes;
using Substrate.Nbt;

namespace NBTUtil.Ops;

internal class JsonOperation : ConsoleOperation
{
    public override bool CanProcess(DataNode dataNode)
    {
        return dataNode is NbtFileDataNode or TagDataNode;
    }

    public override bool Process(DataNode dataNode, ConsoleOptions options)
    {
        if (options.Values.Count == 0)
            return false;

        var jsonPath = options.Values[0];
        using var stream = File.OpenWrite(jsonPath);
        using var writer = new StreamWriter(stream);
        switch (dataNode)
        {
            case TagDataNode node:
            {
                WriteNbtTag(writer, node.Tag);
                break;
            }
            case NbtFileDataNode:
            {
                dataNode.Expand();
                var root = new TagNodeCompound();

                foreach (var child in dataNode.Nodes)
                {
                    if (child is not TagDataNode childTagNode)
                        continue;

                    if (childTagNode.NodeName != null)
                        root.Add(childTagNode.NodeName, childTagNode.Tag);
                }

                WriteNbtTag(writer, root);
                break;
            }
        }

        return true;
    }

    private static void WriteNbtTag(StreamWriter writer, TagNode tag)
    {
        writer.Write(JSONSerializer.Serialize(tag));
    }
}
