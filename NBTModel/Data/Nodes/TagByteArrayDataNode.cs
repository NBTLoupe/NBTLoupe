using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagByteArrayDataNode(TagNodeByteArray tag) : TagDataNode(tag)
{
    private new TagNodeByteArray Tag => (TagNodeByteArray)base.Tag;

    public override bool CanEditNode => true;

    public override string NodeDisplay => NodeDisplayPrefix + Tag.Data.Length + " bytes";

    public override bool EditNode()
    {
        return EditByteHexValue(Tag);
    }
}
