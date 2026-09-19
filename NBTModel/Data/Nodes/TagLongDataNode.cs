using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagLongDataNode(TagNodeLong tag) : TagDataNode(tag)
{
    private new TagNodeLong Tag => (TagNodeLong)base.Tag;

    public override bool EditNode(string value)
    {
        return EditScalarValue(Tag, value);
    }
}
