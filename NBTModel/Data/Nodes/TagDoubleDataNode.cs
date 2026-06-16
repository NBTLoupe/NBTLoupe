using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagDoubleDataNode(TagNodeDouble tag) : TagDataNode(tag)
{
    private new TagNodeDouble Tag => (TagNodeDouble)base.Tag;

    public override bool Parse(string value)
    {
        if (!double.TryParse(value, out var data))
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
