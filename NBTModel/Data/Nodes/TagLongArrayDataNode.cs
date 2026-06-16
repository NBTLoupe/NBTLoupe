using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagLongArrayDataNode(TagNodeLongArray tag) : TagDataNode(tag)
{
    private new TagNodeLongArray Tag => (TagNodeLongArray)base.Tag;

    public override bool CanEditNode
    {
#if WINDOWS
            get { return true; }
#else
        get { return false; }
#endif
    }

    public override string NodeDisplay => NodeDisplayPrefix + Tag.Data.Length + " long integers";

    public override bool EditNode()
    {
        return EditLongHexValue(Tag);
    }
}
