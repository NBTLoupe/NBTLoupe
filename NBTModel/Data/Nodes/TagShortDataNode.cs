using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagShortDataNode(TagNodeShort tag) : TagDataNode(tag)
{
    private new TagNodeShort Tag => (TagNodeShort)base.Tag;

    public override bool Parse(string value)
    {
        if (!short.TryParse(value, out var data))
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
