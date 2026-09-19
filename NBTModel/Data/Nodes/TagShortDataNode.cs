using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagShortDataNode(TagNodeShort tag) : TagDataNode(tag)
{
    private new TagNodeShort Tag => (TagNodeShort)base.Tag;

    public override bool EditNode(string value)
    {
        return EditScalarValue(Tag, value);
    }
}
