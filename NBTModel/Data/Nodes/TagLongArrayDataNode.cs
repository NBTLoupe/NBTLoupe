using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagLongArrayDataNode(TagNodeLongArray tag) : TagDataNode(tag)
{
    private new TagNodeLongArray Tag => (TagNodeLongArray)base.Tag;

    public override bool CanEditNode => true;

    public override string NodeDisplay => NodeDisplayPrefix + Tag.Data.Length + " long integers";

    public override bool EditNode()
    {
        return EditLongHexValue(Tag);
    }
}
