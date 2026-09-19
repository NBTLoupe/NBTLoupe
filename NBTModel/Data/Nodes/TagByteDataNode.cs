using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagByteDataNode(TagNodeByte tag) : TagDataNode(tag)
{
    private new TagNodeByte Tag => (TagNodeByte)base.Tag;

    public override string NodeDisplay => NodeDisplayPrefix + unchecked((sbyte)Tag.Data);

    public override bool EditNode(string value)
    {
        return EditScalarValue(Tag, value);
    }
}
