using System;
using Substrate.Nbt;

namespace NBTModel.Data;

public class ListTagContainer(TagNodeList tag) : IOrderedTagContainer
{
    private readonly Action<bool>? _modifyHandler = null;

    public int TagCount => tag.Count;

    public bool DeleteTag(TagNode tag1)
    {
        var result = tag.Remove(tag1);
        if (result)
            SetModified();

        return result;
    }

    public int GetTagIndex(TagNode tag1)
    {
        return tag.IndexOf(tag1);
    }

    public void InsertTag(TagNode tag1, int index)
    {
        if (index < 0 || index > tag.Count)
            return;

        if (tag.ValueType != tag1.GetTagType())
            return;

        tag.Insert(index, tag1);

        SetModified();
    }

    private void SetModified()
    {
        _modifyHandler?.Invoke(true);
    }
}