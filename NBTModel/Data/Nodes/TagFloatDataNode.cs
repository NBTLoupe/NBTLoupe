using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagFloatDataNode(TagNodeFloat tag) : TagDataNode(tag)
{
    private new TagNodeFloat Tag => (TagNodeFloat)base.Tag;

    public override bool Parse(string value)
    {
        if (!float.TryParse(value, out var data))
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
