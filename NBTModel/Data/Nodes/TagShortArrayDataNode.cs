using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagShortArrayDataNode(TagNodeShortArray tag) : TagDataNode(tag)
{
    private new TagNodeShortArray Tag => (TagNodeShortArray)base.Tag;

    public override bool CanEditNode => true;

    public override string NodeDisplay => NodeDisplayPrefix + Tag.Data.Length + " shorts";

    public override bool EditNode()
    {
        return EditShortHexValue(Tag);
    }
}
