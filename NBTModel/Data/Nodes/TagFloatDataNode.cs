using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagFloatDataNode(TagNodeFloat tag) : TagDataNode(tag)
{
    private new TagNodeFloat Tag => (TagNodeFloat)base.Tag;
    

    public override bool EditNode(string value)
    {
        return EditScalarValue(Tag, value);
    }
}
