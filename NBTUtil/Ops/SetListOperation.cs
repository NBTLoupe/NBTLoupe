using System;
using NBTModel.Data.Nodes;

namespace NBTUtil.Ops;

internal class SetListOperation : ConsoleOperation
{
    public override bool CanProcess(DataNode dataNode)
    {
        return dataNode is TagListDataNode;
    }

    public override bool Process(DataNode dataNode, ConsoleOptions options)
    {
        if (dataNode is not TagListDataNode listNode) throw new NullReferenceException();

        listNode.Clear();
        foreach (var value in options.Values)
        {
            var tag = TagDataNode.DefaultTag(listNode.Tag.ValueType);
            var tagData = TagDataNode.CreateFromTag(tag);
            if (tagData?.Parse(value) != true)
                return false;

            if (!listNode.AppendTag(tagData.Tag))
                return false;
        }

        return true;
    }
}