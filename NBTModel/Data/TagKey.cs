using System;
using Substrate.Nbt;

namespace NBTModel.Data;

public class TagKey(string name, TagType type) : IComparable<TagKey>
{
    private string Name { get; } = name;

    private TagType TagType { get; } = type;

    #region IComparable<TagKey> Members

    public int CompareTo(TagKey? other)
    {
        return Compare(this, other ?? throw new ArgumentNullException(nameof(other)));
    }

    #endregion

    #region IComparer<TagKey> Members

    private static int Compare(TagKey x, TagKey y)
    {
        var typeDiff = (int)x.TagType - (int)y.TagType;
        return typeDiff != 0 ? typeDiff : string.CompareOrdinal(x.Name, y.Name);
    }

    #endregion
}