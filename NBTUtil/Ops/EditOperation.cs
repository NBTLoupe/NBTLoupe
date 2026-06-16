using NBTModel.Data.Nodes;

namespace NBTUtil.Ops;

internal class EditOperation : ConsoleOperation
{
    public override bool OptionsValid(ConsoleOptions options)
    {
        return options.Values.Count != 0;
    }

    public override bool CanProcess(DataNode dataNode)
    {
        if (dataNode is not TagDataNode || !dataNode.CanEditNode)
            return false;
        return dataNode is not TagByteArrayDataNode && dataNode is not TagIntArrayDataNode &&
               dataNode is not TagShortArrayDataNode && dataNode is not TagLongArrayDataNode;
    }

    public override bool Process(DataNode dataNode, ConsoleOptions options)
    {
        var value = options.Values[0];

        var tagDataNode = dataNode as TagDataNode;
        return tagDataNode != null && tagDataNode.Parse(value);
    }
}