using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class TagCompoundDataNode(TagNodeCompound tag) : TagDataNode.Container(tag)
{
    private readonly CompoundTagContainer _container = new(tag);

    private new TagNodeCompound Tag => (TagNodeCompound)base.Tag;

    public override bool IsNamedContainer => true;

    public override INamedTagContainer NamedTagContainer => _container;

    public override int TagCount => _container.TagCount;

    protected override void ExpandCore()
    {
        var list = new SortedList<TagKey, TagNode>();
        foreach (var item in Tag) list.Add(new TagKey(item.Key, item.Value.GetTagType()), item.Value);

        foreach (var node in list.Select(item => CreateFromTag(item.Value)))
            if (node != null)
                Nodes.Add(node);
    }

    public override bool CanCreateTag(TagType type)
    {
        return Enum.IsDefined(type) && type != TagType.TAG_END;
    }

    public override async Task<bool> CanPasteIntoNode()
    {
        return await NbtClipboardController.ContainsDataAsync();
    }

    public override bool CreateNode(TagType type)
    {
        if (!CanCreateTag(type))
            return false;

        if (FormRegistry.CreateNode == null) return false;
        var data = new CreateTagFormData
        {
            TagType = type
        };

        if (!FormRegistry.CreateNode(data)) return false;

        if (data.TagNode == null || data.TagName == null) return false;
        AddTag(data.TagNode, data.TagName);
        return true;
    }

    public override async Task<bool> PasteNode()
    {
        if (!await CanPasteIntoNode())
            return false;

        var clipboard = await NbtClipboardController.CopyFromClipboardAsync();
        if (clipboard?.Node == null)
            return false;

        var name = clipboard.Name;
        if (string.IsNullOrEmpty(name))
            name = "UNNAMED";

        AddTag(clipboard.Node, MakeUniqueName(name));
        return true;
    }

    public override bool DeleteTag(TagNode tag)
    {
        return _container.DeleteTag(tag);
    }

    public override void SyncTag()
    {
        var lookup = new Dictionary<TagNode, TagDataNode>();
        foreach (var dataNode in Nodes)
        {
            var node = (TagDataNode)dataNode;
            lookup[node.Tag] = node;
        }

        foreach (var kvp in lookup.Where(kvp => !Tag.Values.Contains(kvp.Key))) Nodes.Remove(kvp.Value);

        foreach (var tag in Tag.Values)
        {
            if (lookup.ContainsKey(tag)) continue;
            var newnode = CreateFromTag(tag);
            if (newnode == null) continue;
            Nodes.Add(newnode);
            newnode.Expand();
        }

        foreach (var dataNode in Nodes)
        {
            var node = (TagDataNode)dataNode;
            node.SyncTag();
        }
    }

    private void AddTag(TagNode tag, string name)
    {
        _container.AddTag(tag, name);
        IsDataModified = true;

        if (!IsExpanded) return;
        var node = CreateFromTag(tag);
        if (node != null)
            Nodes.Add(node);
    }

    private string MakeUniqueName(string name)
    {
        var names = new List<string>(_container.TagNamesInUse);
        if (!names.Contains(name))
            return name;

        var index = 1;
        while (names.Contains(MakeCandidateName(name, index)))
            index++;

        return MakeCandidateName(name, index);
    }

    private static string MakeCandidateName(string name, int index)
    {
        return name + " (Copy " + index + ")";
    }
}