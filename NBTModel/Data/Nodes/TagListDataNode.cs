using System;
using System.Threading.Tasks;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public sealed class TagListDataNode(TagNodeList tag) : TagDataNode.Container(tag)
{
    private readonly ListTagContainer _container = new(tag);

    public new TagNodeList Tag => (TagNodeList)base.Tag;

    public override bool IsOrderedContainer => true;

    public override IOrderedTagContainer OrderedTagContainer => _container;

    public override int TagCount => _container.TagCount;

    protected override void ExpandCore()
    {
        foreach (var tag in Tag)
        {
            var node = CreateFromTag(tag);
            if (node != null)
                Nodes.Add(node);
        }
    }

    public override bool CanCreateTag(TagType type)
    {
        if (Tag.Count > 0)
            return Tag.ValueType == type;
        return Enum.IsDefined(type) && type != TagType.TAG_END;
    }

    public override async Task<bool> CanPasteIntoNode()
    {
        if (!await NbtClipboardController.ContainsDataAsync()) return false;
        var data = await NbtClipboardController.CopyFromClipboardAsync();
        if (data == null)
            return false;

        return data.Node.GetTagType() == Tag.ValueType || Tag.Count == 0;
    }

    public override bool CreateNode(TagType type)
    {
        if (!CanCreateTag(type))
            return false;

        if (Tag.Count == 0) Tag.ChangeValueType(type);

        AppendTag(DefaultTag(type));
        return true;
    }

    public override async Task<bool> PasteNode()
    {
        if (!await CanPasteIntoNode())
            return false;

        var clipboard = await NbtClipboardController.CopyFromClipboardAsync();
        if (clipboard?.Node == null)
            return false;

        if (Tag.Count == 0) Tag.ChangeValueType(clipboard.Node.GetTagType());

        AppendTag(clipboard.Node);
        return true;
    }

    public override bool DeleteTag(TagNode tag)
    {
        return _container.DeleteTag(tag);
    }

    public void Clear()
    {
        if (TagCount == 0)
            return;

        Nodes.Clear();
        Tag.Clear();

        IsDataModified = true;
    }

    public bool AppendTag(TagNode tag)
    {
        if (!CanCreateTag(tag.GetTagType()))
            return false;

        _container.InsertTag(tag, _container.TagCount);
        IsDataModified = true;

        if (!IsExpanded) return true;
        var node = CreateFromTag(tag);
        if (node != null)
            Nodes.Add(node);

        return true;
    }
}
