using System.Collections.Generic;
using Substrate.Nbt;

namespace NBTModel.Data;

public interface ITagContainer
{
    int TagCount { get; }

    bool DeleteTag(TagNode tag);
}

public interface IMetaTagContainer : ITagContainer
{
    bool IsNamedContainer { get; }
    bool IsOrderedContainer { get; }

    INamedTagContainer? NamedTagContainer { get; }
    IOrderedTagContainer? OrderedTagContainer { get; }
}

public interface INamedTagContainer : ITagContainer
{
    IEnumerable<string> TagNamesInUse { get; }

    string GetTagName(TagNode tag);

    bool RenameTag(TagNode tag, string name);
}

public interface IOrderedTagContainer : ITagContainer
{
    int GetTagIndex(TagNode tag);
    void InsertTag(TagNode tag, int index);
}