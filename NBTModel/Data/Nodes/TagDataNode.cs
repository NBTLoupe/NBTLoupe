using System;
using System.Threading.Tasks;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTExplorer.Model
{
    public abstract class TagDataNode : DataNode
    {
        public abstract class Container : TagDataNode, IMetaTagContainer
        {
            protected Container (TagNode tag)
                : base(tag)
            { }

            #region ITagContainer

            public virtual int TagCount
            {
                get { return 0; }
            }

            public virtual bool IsNamedContainer
            {
                get { return false; }
            }

            public virtual bool IsOrderedContainer
            {
                get { return false; }
            }

            public virtual INamedTagContainer NamedTagContainer
            {
                get { return null; }
            }

            public virtual IOrderedTagContainer OrderedTagContainer
            {
                get { return null; }
            }

            public virtual bool DeleteTag (TagNode tag)
            {
                return false;
            }

            public virtual void Clear ()
            { }

            #endregion

            protected override NodeCapabilities Capabilities
            {
                get
                {
                    return NodeCapabilities.Copy
                        | NodeCapabilities.CreateTag
                        | NodeCapabilities.Cut
                        | NodeCapabilities.Delete
                        | NodeCapabilities.PasteInto
                        | ((TagParent != null && TagParent.IsNamedContainer) ? NodeCapabilities.Rename : NodeCapabilities.None)
                        | ((TagParent != null && TagParent.IsOrderedContainer) ? NodeCapabilities.Reorder : NodeCapabilities.None)
                        | NodeCapabilities.Search;
                }
            }

            public override bool HasUnexpandedChildren
            {
                get { return !IsExpanded && TagCount > 0; }
            }

            public override bool IsContainerType
            {
                get { return true; }
            }

            public override string NodeDisplay
            {
                get { return NodeDisplayPrefix + TagCount + ((TagCount == 1) ? " entry" : " entries"); }
            }
        }

        static public TagDataNode CreateFromTag (TagNode tag)
        {
            return tag == null
                ? null
                : tag.GetTagType() switch
                {
                    TagType.TAG_BYTE => new TagByteDataNode(tag as TagNodeByte),
                    TagType.TAG_BYTE_ARRAY => new TagByteArrayDataNode(tag as TagNodeByteArray),
                    TagType.TAG_COMPOUND => new TagCompoundDataNode(tag as TagNodeCompound),
                    TagType.TAG_DOUBLE => new TagDoubleDataNode(tag as TagNodeDouble),
                    TagType.TAG_FLOAT => new TagFloatDataNode(tag as TagNodeFloat),
                    TagType.TAG_INT => new TagIntDataNode(tag as TagNodeInt),
                    TagType.TAG_INT_ARRAY => new TagIntArrayDataNode(tag as TagNodeIntArray),
                    TagType.TAG_LIST => new TagListDataNode(tag as TagNodeList),
                    TagType.TAG_LONG => new TagLongDataNode(tag as TagNodeLong),
                    TagType.TAG_LONG_ARRAY => new TagLongArrayDataNode(tag as TagNodeLongArray),
                    TagType.TAG_SHORT => new TagShortDataNode(tag as TagNodeShort),
                    TagType.TAG_SHORT_ARRAY => new TagShortArrayDataNode(tag as TagNodeShortArray),
                    TagType.TAG_STRING => new TagStringDataNode(tag as TagNodeString),
                    _ => null
                };
        }

        static public TagNode DefaultTag (TagType type)
        {
            switch (type) {
                case TagType.TAG_BYTE:
                    return new TagNodeByte(0);
                case TagType.TAG_BYTE_ARRAY:
                    return new TagNodeByteArray(new byte[0]);
                case TagType.TAG_COMPOUND:
                    return new TagNodeCompound();
                case TagType.TAG_DOUBLE:
                    return new TagNodeDouble(0);
                case TagType.TAG_FLOAT:
                    return new TagNodeFloat(0);
                case TagType.TAG_INT:
                    return new TagNodeInt(0);
                case TagType.TAG_INT_ARRAY:
                    return new TagNodeIntArray(new int[0]);
                case TagType.TAG_LIST:
                    return new TagNodeList(TagType.TAG_BYTE);
                case TagType.TAG_LONG:
                    return new TagNodeLong(0);
                case TagType.TAG_LONG_ARRAY:
                    return new TagNodeLongArray(new long[0]);
                case TagType.TAG_SHORT:
                    return new TagNodeShort(0);
                case TagType.TAG_SHORT_ARRAY:
                    return new TagNodeShortArray(new short[0]);
                case TagType.TAG_STRING:
                    return new TagNodeString("");
                default:
                    return new TagNodeByte(0);
            }
        }

        private TagNode _tag;

        protected TagDataNode (TagNode tag)
        {
            _tag = tag;
        }

        protected IMetaTagContainer TagParent
        {
            get { return base.Parent as IMetaTagContainer; }
        }

        public TagNode Tag
        {
            get { return _tag; }
            protected set
            {
                if (_tag.GetTagType() == value.GetTagType())
                    _tag = value;
            }
        }

        public virtual bool Parse (string value)
        {
            return false;
        }

        protected override NodeCapabilities Capabilities
        {
            get
            {
                return NodeCapabilities.Copy
                    | NodeCapabilities.Cut
                    | NodeCapabilities.Delete
                    | NodeCapabilities.Edit
                    | ((TagParent != null && TagParent.IsNamedContainer) ? NodeCapabilities.Rename : NodeCapabilities.None)
                    | ((TagParent != null && TagParent.IsOrderedContainer) ? NodeCapabilities.Reorder : NodeCapabilities.None);
            }
        }

        public override bool CanMoveNodeUp
        {
            get
            {
                if (TagParent != null && TagParent.IsOrderedContainer)
                    return TagParent.OrderedTagContainer.GetTagIndex(Tag) > 0;
                return false;
            }
        }

        public override bool CanMoveNodeDown
        {
            get
            {
                if (TagParent != null && TagParent.IsOrderedContainer)
                    return TagParent.OrderedTagContainer.GetTagIndex(Tag) < (TagParent.TagCount - 1);
                return false;
            }
        }

        public override string NodeName
        {
            get
            {
                if (TagParent == null || !TagParent.IsNamedContainer)
                    return null;

                return TagParent.NamedTagContainer.GetTagName(Tag);
            }
        }

        public override string NodePathName
        {
            get
            {
                if (Parent is TagDataNode.Container) {
                    TagDataNode.Container container = Parent as TagDataNode.Container;
                    if (container.IsOrderedContainer)
                        return container.OrderedTagContainer.GetTagIndex(Tag).ToString();
                }

                return base.NodePathName;
            }
        }

        protected string NodeDisplayPrefix
        {
            get
            {
                string name = NodeName;
                return String.IsNullOrEmpty(name) ? "" : name + ": ";
            }
        }

        public override string NodeDisplay
        {
            get { return NodeDisplayPrefix + Tag.ToString(); }
        }

        public override bool DeleteNode ()
        {
            if (CanDeleteNode && TagParent != null) {
                TagParent.DeleteTag(Tag);
                IsParentModified = true;
                return Parent.Nodes.Remove(this);
            }

            return false;
        }

        public override bool RenameNode ()
        {
            if (CanRenameNode && TagParent != null && TagParent.IsNamedContainer && FormRegistry.RenameTag != null) {
                RestrictedStringFormData data = new RestrictedStringFormData(TagParent.NamedTagContainer.GetTagName(Tag));
                data.RestrictedValues.AddRange(TagParent.NamedTagContainer.TagNamesInUse);

                if (FormRegistry.RenameTag(data)) {
                    if (TagParent.NamedTagContainer.RenameTag(Tag, data.Value)) {
                        IsDataModified = true;
                        return true;
                    }
                }
            }

            return false;
        }

        public override async Task<bool> CopyNode ()
        {
            if (CanCopyNode) {
                await NbtClipboardController.CopyToClipboardAsync(new NbtClipboardData(NodeName, Tag));
                return true;
            }

            return false;
        }

        public override async Task<bool> CutNode ()
        {
            if (CanCutNode && TagParent != null) {
                await NbtClipboardController.CopyToClipboardAsync(new NbtClipboardData(NodeName, Tag));

                TagParent.DeleteTag(Tag);
                IsParentModified = true;
                Parent.Nodes.Remove(this);
                return true;
            }

            return false;
        }

        public override bool ChangeRelativePosition (int offset)
        {
            if (CanReoderNode && TagParent != null) {
                int curIndex = TagParent.OrderedTagContainer.GetTagIndex(Tag);
                int newIndex = curIndex + offset;

                if (newIndex < 0 || newIndex >= TagParent.OrderedTagContainer.TagCount)
                    return false;

                TagParent.OrderedTagContainer.DeleteTag(Tag);
                TagParent.OrderedTagContainer.InsertTag(Tag, newIndex);

                DataNode parent = Parent;
                parent.Nodes.Remove(this);
                parent.Nodes.Insert(newIndex, this);
                IsParentModified = true;
                return true;
            }

            return false;
        }

        protected bool EditScalarValue (TagNode tag)
        {
            if (FormRegistry.EditTagScalar != null) {
                if (FormRegistry.EditTagScalar(new TagScalarFormData(tag))) {
                    IsDataModified = true;
                    return true;
                }
            }
            return false;
        }

        protected bool EditStringValue (TagNode tag)
        {
            if (FormRegistry.EditString != null) {
                StringFormData data = new StringFormData(tag.ToTagString().Data);
                if (FormRegistry.EditString(data)) {
                    tag.ToTagString().Data = data.Value;
                    IsDataModified = true;
                    return true;
                }
            }
            return false;
        }

        protected bool EditByteHexValue (TagNode tag)
        {
            if (FormRegistry.EditByteArray != null) {
                byte[] byteData = new byte[tag.ToTagByteArray().Length];
                Array.Copy(tag.ToTagByteArray().Data, byteData, byteData.Length);

                ByteArrayFormData data = new ByteArrayFormData() {
                    NodeName = NodeName,
                    BytesPerElement = 1,
                    Data = byteData,
                };

                if (FormRegistry.EditByteArray(data)) {
                    tag.ToTagByteArray().Data = data.Data;
                    //Array.Copy(data.Data, tag.ToTagByteArray().Data, tag.ToTagByteArray().Length);
                    IsDataModified = true;
                    return true;
                }
            }

            return false;
        }

        protected bool EditShortHexValue (TagNode tag)
        {
            if (FormRegistry.EditByteArray != null) {
                TagNodeShortArray iatag = tag.ToTagShortArray();
                byte[] byteData = new byte[iatag.Length * 2];
                for (int i = 0; i < iatag.Length; i++) {
                    byte[] buf = BitConverter.GetBytes(iatag.Data[i]);
                    Array.Copy(buf, 0, byteData, 2 * i, 2);
                }

                ByteArrayFormData data = new ByteArrayFormData() {
                    NodeName = NodeName,
                    BytesPerElement = 2,
                    Data = byteData,
                };

                if (FormRegistry.EditByteArray(data)) {
                    iatag.Data = new short[data.Data.Length / 2];
                    for (int i = 0; i < iatag.Length; i++) {
                        iatag.Data[i] = BitConverter.ToInt16(data.Data, i * 2);
                    }

                    IsDataModified = true;
                    return true;
                }
            }

            return false;
        }

        protected bool EditIntHexValue (TagNode tag)
        {
            if (FormRegistry.EditByteArray != null) {
                TagNodeIntArray iatag = tag.ToTagIntArray();
                byte[] byteData = new byte[iatag.Length * 4];
                for (int i = 0; i < iatag.Length; i++) {
                    byte[] buf = BitConverter.GetBytes(iatag.Data[i]);
                    Array.Copy(buf, 0, byteData, 4 * i, 4);
                }

                ByteArrayFormData data = new ByteArrayFormData() {
                    NodeName = NodeName,
                    BytesPerElement = 4,
                    Data = byteData,
                };

                if (FormRegistry.EditByteArray(data)) {
                    iatag.Data = new int[data.Data.Length / 4];
                    for (int i = 0; i < iatag.Length; i++) {
                        iatag.Data[i] = BitConverter.ToInt32(data.Data, i * 4);
                    }

                    IsDataModified = true;
                    return true;
                }
            }

            return false;
        }

        protected bool EditLongHexValue(TagNode tag)
        {
            if (FormRegistry.EditByteArray != null)
            {
                TagNodeLongArray latag = tag.ToTagLongArray();
                byte[] byteData = new byte[latag.Length * 8];
                for (int i = 0; i < latag.Length; i++)
                {
                    byte[] buf = BitConverter.GetBytes(latag.Data[i]);
                    Array.Copy(buf, 0, byteData, 8 * i, 8);
                }

                ByteArrayFormData data = new ByteArrayFormData()
                {
                    NodeName = NodeName,
                    BytesPerElement = 8,
                    Data = byteData,
                };

                if (FormRegistry.EditByteArray(data))
                {
                    latag.Data = new long[data.Data.Length / 8];
                    for (int i = 0; i < latag.Length; i++)
                    {
                        latag.Data[i] = BitConverter.ToInt64(data.Data, i * 8);
                    }

                    IsDataModified = true;
                    return true;
                }
            }

            return false;
        }

        public virtual void SyncTag ()
        {
        }
    }
}
