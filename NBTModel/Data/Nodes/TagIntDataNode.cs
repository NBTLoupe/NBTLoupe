using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagIntDataNode(TagNodeInt tag) : TagDataNode(tag)
{
    private new TagNodeInt Tag => (TagNodeInt)base.Tag;

    public override bool EditNode(string value)
    {
        return EditScalarValue(Tag, value);
    }
}
