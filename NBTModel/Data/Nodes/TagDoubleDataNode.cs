using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagDoubleDataNode(TagNodeDouble tag) : TagDataNode(tag)
{
    private new TagNodeDouble Tag => (TagNodeDouble)base.Tag;

    public override bool EditNode(string value)
    {
        return EditScalarValue(Tag, value);
    }
}
