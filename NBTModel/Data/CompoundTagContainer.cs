using System.Collections.Generic;
using System.Linq;
using Substrate.Nbt;

namespace NBTModel.Data;

public class CompoundTagContainer(TagNodeCompound tag) : INamedTagContainer
{
    public int TagCount => tag.Count;

    public IEnumerable<string> TagNamesInUse => tag.Keys;

    public string GetTagName(TagNode tag1)
    {
        return tag.Keys.First(name => tag[name] == tag1);
    }

    public bool RenameTag(TagNode tag1, string name)
    {
        if (tag.ContainsKey(name))
            return false;

        var oldName = GetTagName(tag1);
        tag.Remove(oldName);
        tag.Add(name, tag1);

        return true;
    }

    public bool DeleteTag(TagNode tag1)
    {
        return (from name in tag.Keys where tag[name] == tag1 select tag.Remove(name)).FirstOrDefault();
    }

    public void AddTag(TagNode tag1, string name)
    {
        tag.TryAdd(name, tag1);
    }
}