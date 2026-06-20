using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagIntArrayDataNode(TagNodeIntArray tag) : TagDataNode(tag)
{
    private new TagNodeIntArray Tag => (TagNodeIntArray)base.Tag;

    public override bool CanEditNode => true;

    public override string NodeDisplay => NodeDisplayPrefix + Tag.Data.Length + " integers";

    public override bool EditNode()
    {
        return EditIntHexValue(Tag);
    }
}
