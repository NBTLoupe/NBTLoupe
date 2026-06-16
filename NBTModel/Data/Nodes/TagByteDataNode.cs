using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagByteDataNode(TagNodeByte tag) : TagDataNode(tag)
{
    private new TagNodeByte Tag => (TagNodeByte)base.Tag;

    public override string NodeDisplay => NodeDisplayPrefix + unchecked((sbyte)Tag.Data);

    public override bool Parse(string value)
    {
        if (!byte.TryParse(value, out var data))
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