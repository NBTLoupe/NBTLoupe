using System;
using System.Threading.Tasks;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public abstract class TagDataNode(TagNode tag) : DataNode
{
    private IMetaTagContainer? TagParent => (IMetaTagContainer?)Parent;

    public TagNode Tag { get; } = tag;

    protected override NodeCapabilities Capabilities =>
        NodeCapabilities.Copy
        | NodeCapabilities.Cut
        | NodeCapabilities.Delete
        | NodeCapabilities.Edit
        | (TagParent is { IsNamedContainer: true } ? NodeCapabilities.Rename : NodeCapabilities.None)
        | (TagParent is { IsOrderedContainer: true } ? NodeCapabilities.Reorder : NodeCapabilities.None);

    public override bool CanMoveNodeUp
    {
        get
        {
            if (TagParent is { IsOrderedContainer: true })
                return TagParent.OrderedTagContainer?.GetTagIndex(Tag) > 0;
            return false;
        }
    }

    public override bool CanMoveNodeDown
    {
        get
        {
            if (TagParent is { IsOrderedContainer: true })
                return TagParent.OrderedTagContainer?.GetTagIndex(Tag) < TagParent.TagCount - 1;
            return false;
        }
    }

    public override string NodeName => (TagParent is not { IsNamedContainer: true }
        ? null
        : TagParent.NamedTagContainer?.GetTagName(Tag)) ?? "";

    public override string NodePathName
    {
        get
        {
            if (Parent is not Container) return base.NodePathName;
            if (Parent is Container { IsOrderedContainer: true } container)
                return container.OrderedTagContainer?.GetTagIndex(Tag).ToString() ?? base.NodePathName;

            return base.NodePathName;
        }
    }

    protected string NodeDisplayPrefix
    {
        get
        {
            var name = NodeName;
            return string.IsNullOrEmpty(name) ? "" : name + ": ";
        }
    }

    public override string NodeDisplay => NodeDisplayPrefix + Tag;

    public static TagDataNode? CreateFromTag(TagNode tag)
    {
        return tag.GetTagType() switch
        {
            TagType.TAG_BYTE => new TagByteDataNode((TagNodeByte)tag),
            TagType.TAG_BYTE_ARRAY => new TagByteArrayDataNode((TagNodeByteArray)tag),
            TagType.TAG_COMPOUND => new TagCompoundDataNode((TagNodeCompound)tag),
            TagType.TAG_DOUBLE => new TagDoubleDataNode((TagNodeDouble)tag),
            TagType.TAG_FLOAT => new TagFloatDataNode((TagNodeFloat)tag),
            TagType.TAG_INT => new TagIntDataNode((TagNodeInt)tag),
            TagType.TAG_INT_ARRAY => new TagIntArrayDataNode((TagNodeIntArray)tag),
            TagType.TAG_LIST => new TagListDataNode((TagNodeList)tag),
            TagType.TAG_LONG => new TagLongDataNode((TagNodeLong)tag),
            TagType.TAG_LONG_ARRAY => new TagLongArrayDataNode((TagNodeLongArray)tag),
            TagType.TAG_SHORT => new TagShortDataNode((TagNodeShort)tag),
            TagType.TAG_SHORT_ARRAY => new TagShortArrayDataNode((TagNodeShortArray)tag),
            TagType.TAG_STRING => new TagStringDataNode((TagNodeString)tag),
            _ => null
        };
    }

    public static TagNode DefaultTag(TagType type)
    {
        return type switch
        {
            TagType.TAG_BYTE => new TagNodeByte(0),
            TagType.TAG_BYTE_ARRAY => new TagNodeByteArray([]),
            TagType.TAG_COMPOUND => new TagNodeCompound(),
            TagType.TAG_DOUBLE => new TagNodeDouble(0),
            TagType.TAG_FLOAT => new TagNodeFloat(0),
            TagType.TAG_INT => new TagNodeInt(0),
            TagType.TAG_INT_ARRAY => new TagNodeIntArray([]),
            TagType.TAG_LIST => new TagNodeList(TagType.TAG_BYTE),
            TagType.TAG_LONG => new TagNodeLong(0),
            TagType.TAG_LONG_ARRAY => new TagNodeLongArray([]),
            TagType.TAG_SHORT => new TagNodeShort(0),
            TagType.TAG_SHORT_ARRAY => new TagNodeShortArray([]),
            TagType.TAG_STRING => new TagNodeString(""),
            _ => new TagNodeByte(0)
        };
    }

    public virtual bool Parse(string value)
    {
        return false;
    }

    public override bool DeleteNode()
    {
        if (!CanDeleteNode) return false;
        TagParent?.DeleteTag(Tag);
        IsParentModified = true;
        return Parent?.Nodes.Remove(this) == true;
    }

    public override bool RenameNode()
    {
        if (!CanRenameNode || TagParent is not { IsNamedContainer: true } || FormRegistry.RenameTag == null)
            return false;
        var tagName = TagParent.NamedTagContainer?.GetTagName(Tag);
        if (tagName == null) return false;
        var data = new RestrictedStringFormData(tagName);

        if (!FormRegistry.RenameTag(data)) return false;
        if (TagParent.NamedTagContainer?.RenameTag(Tag, data.Value) != true) return false;
        IsDataModified = true;
        return true;
    }

    public override async Task<bool> CopyNode()
    {
        if (!CanCopyNode) return false;
        await NbtClipboardController.CopyToClipboardAsync(new NbtClipboardData(NodeName, Tag));
        return true;
    }

    public override async Task<bool> CutNode()
    {
        if (!CanCutNode) return false;
        await NbtClipboardController.CopyToClipboardAsync(new NbtClipboardData(NodeName, Tag));

        TagParent?.DeleteTag(Tag);
        IsParentModified = true;
        Parent?.Nodes.Remove(this);
        return true;
    }

    public override bool ChangeRelativePosition(int offset)
    {
        if (!CanReoderNode) return false;
        var curIndex = TagParent?.OrderedTagContainer?.GetTagIndex(Tag);
        var newIndex = curIndex + offset ?? -1;

        if (newIndex < 0 || newIndex >= TagParent?.OrderedTagContainer?.TagCount)
            return false;

        TagParent?.OrderedTagContainer?.DeleteTag(Tag);
        TagParent?.OrderedTagContainer?.InsertTag(Tag, newIndex);

        var parent = Parent;
        parent?.Nodes.Remove(this);
        parent?.Nodes.Insert(newIndex, this);
        IsParentModified = true;
        return true;
    }

    protected bool EditScalarValue(TagNode tag)
    {
        if (FormRegistry.EditTagScalar == null) return false;
        if (!FormRegistry.EditTagScalar(new TagScalarFormData(tag))) return false;
        IsDataModified = true;
        return true;
    }

    protected bool EditStringValue(TagNode tag)
    {
        if (FormRegistry.EditString == null) return false;
        var data = new StringFormData(tag.ToTagString().Data);
        if (!FormRegistry.EditString(data)) return false;
        tag.ToTagString().Data = data.Value;
        IsDataModified = true;
        return true;
    }

    protected bool EditByteHexValue(TagNode tag)
    {
        if (FormRegistry.EditByteArray == null) return false;
        var byteData = new byte[tag.ToTagByteArray().Length];
        Array.Copy(tag.ToTagByteArray().Data, byteData, byteData.Length);

        var data = new ByteArrayFormData
        {
            Data = byteData
        };

        if (!FormRegistry.EditByteArray(data)) return false;
        tag.ToTagByteArray().Data = data.Data;
        IsDataModified = true;
        return true;
    }

    protected bool EditShortHexValue(TagNode tag)
    {
        if (FormRegistry.EditByteArray == null) return false;
        var iatag = tag.ToTagShortArray();
        var byteData = new byte[iatag.Length * 2];
        for (var i = 0; i < iatag.Length; i++)
        {
            var buf = BitConverter.GetBytes(iatag.Data[i]);
            Array.Copy(buf, 0, byteData, 2 * i, 2);
        }

        var data = new ByteArrayFormData
        {
            Data = byteData
        };

        if (!FormRegistry.EditByteArray(data)) return false;
        {
            iatag.Data = new short[data.Data.Length / 2];
            for (var i = 0; i < iatag.Length; i++) iatag.Data[i] = BitConverter.ToInt16(data.Data, i * 2);

            IsDataModified = true;
            return true;
        }
    }

    protected bool EditIntHexValue(TagNode tag)
    {
        if (FormRegistry.EditByteArray == null) return false;
        var iatag = tag.ToTagIntArray();
        var byteData = new byte[iatag.Length * 4];
        for (var i = 0; i < iatag.Length; i++)
        {
            var buf = BitConverter.GetBytes(iatag.Data[i]);
            Array.Copy(buf, 0, byteData, 4 * i, 4);
        }

        var data = new ByteArrayFormData
        {
            Data = byteData
        };

        if (!FormRegistry.EditByteArray(data)) return false;
        {
            iatag.Data = new int[data.Data.Length / 4];
            for (var i = 0; i < iatag.Length; i++) iatag.Data[i] = BitConverter.ToInt32(data.Data, i * 4);

            IsDataModified = true;
            return true;
        }
    }

    protected bool EditLongHexValue(TagNode tag)
    {
        if (FormRegistry.EditByteArray == null) return false;
        var latag = tag.ToTagLongArray();
        var byteData = new byte[latag.Length * 8];
        for (var i = 0; i < latag.Length; i++)
        {
            var buf = BitConverter.GetBytes(latag.Data[i]);
            Array.Copy(buf, 0, byteData, 8 * i, 8);
        }

        var data = new ByteArrayFormData
        {
            Data = byteData
        };

        if (!FormRegistry.EditByteArray(data)) return false;
        {
            latag.Data = new long[data.Data.Length / 8];
            for (var i = 0; i < latag.Length; i++) latag.Data[i] = BitConverter.ToInt64(data.Data, i * 8);

            IsDataModified = true;
            return true;
        }
    }

    public virtual void SyncTag()
    {
    }

    public abstract class Container(TagNode tag) : TagDataNode(tag), IMetaTagContainer
    {
        protected override NodeCapabilities Capabilities =>
            NodeCapabilities.Copy
            | NodeCapabilities.CreateTag
            | NodeCapabilities.Cut
            | NodeCapabilities.Delete
            | NodeCapabilities.PasteInto
            | (TagParent is { IsNamedContainer: true } ? NodeCapabilities.Rename : NodeCapabilities.None)
            | (TagParent is { IsOrderedContainer: true } ? NodeCapabilities.Reorder : NodeCapabilities.None)
            | NodeCapabilities.Search;

        public override bool HasUnexpandedChildren => !IsExpanded && TagCount > 0;

        public override bool IsContainerType => true;

        public override string NodeDisplay => NodeDisplayPrefix + TagCount + (TagCount == 1 ? " entry" : " entries");

        #region ITagContainer

        public virtual int TagCount => 0;

        public virtual bool IsNamedContainer => false;

        public virtual bool IsOrderedContainer => false;

        public virtual INamedTagContainer? NamedTagContainer => null;

        public virtual IOrderedTagContainer? OrderedTagContainer => null;

        public virtual bool DeleteTag(TagNode tag)
        {
            return false;
        }

        #endregion
    }
}