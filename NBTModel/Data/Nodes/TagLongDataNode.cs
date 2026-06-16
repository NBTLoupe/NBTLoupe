using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagLongDataNode(TagNodeLong tag) : TagDataNode(tag)
{
    private new TagNodeLong Tag => (TagNodeLong)base.Tag;

    public override bool Parse(string value)
    {
        if (!long.TryParse(value, out var data))
            return false;

        Tag.Data = data;
        IsDataModified = true;

        return true;
    }

    public override bool EditNode()
    {
        return EditScalarValue(Tag);
    }
}