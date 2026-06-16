using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagStringDataNode(TagNodeString tag) : TagDataNode(tag)
{
    private new TagNodeString Tag => (TagNodeString)base.Tag;

    public override string NodeDisplay => NodeDisplayPrefix + Tag.ToString().Replace('\n', (char)0x00B6);

    public override bool Parse(string value)
    {
        Tag.Data = value;
        IsDataModified = true;

        return true;
    }

    public override bool EditNode()
    {
        return EditStringValue(Tag);
    }
}
