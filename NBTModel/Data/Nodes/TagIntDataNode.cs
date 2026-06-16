using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagIntDataNode(TagNodeInt tag) : TagDataNode(tag)
{
    private new TagNodeInt Tag => (TagNodeInt)base.Tag;

    public override bool Parse(string value)
    {
        if (!int.TryParse(value, out var data))
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
