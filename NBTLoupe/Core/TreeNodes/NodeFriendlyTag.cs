using Substrate.Nbt;

namespace NBTLoupe.Core.TreeNodes;

internal static class NodeFriendlyTag
{
    internal static string GetFriendlyTag(this TagType tagType)
    {
        return tagType switch
        {
            TagType.TAG_BYTE => "Byte Tag",
            TagType.TAG_SHORT => "Short Tag",
            TagType.TAG_INT => "Int Tag",
            TagType.TAG_LONG => "Long Tag",
            TagType.TAG_FLOAT => "Float Tag",
            TagType.TAG_DOUBLE => "Double Tag",

            TagType.TAG_BYTE_ARRAY => "Byte Array Tag",
            TagType.TAG_SHORT_ARRAY => "Short Array Tag",
            TagType.TAG_INT_ARRAY => "Int Array Tag",
            TagType.TAG_LONG_ARRAY => "Long Array Tag",

            TagType.TAG_STRING => "String Tag",
            TagType.TAG_LIST => "List Tag",

            TagType.TAG_COMPOUND => "Compound Tag",

            _ => tagType.ToString()
        };
    }
}
