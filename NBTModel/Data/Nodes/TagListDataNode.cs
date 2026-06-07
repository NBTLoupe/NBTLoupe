using System;
using System.Threading.Tasks;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTExplorer.Model
{
    public class TagListDataNode : TagDataNode.Container
    {
        private ListTagContainer _container;

        public TagListDataNode (TagNodeList tag)
            : base(tag)
        {
            _container = new ListTagContainer(tag, res => IsDataModified = true);
        }

        public new TagNodeList Tag
        {
            get { return base.Tag as TagNodeList; }
            set { base.Tag = value; }
        }

        protected override void ExpandCore ()
        {
            foreach (TagNode tag in Tag) {
                TagDataNode node = TagDataNode.CreateFromTag(tag);
                if (node != null)
                    Nodes.Add(node);
            }
        }

        public override bool CanCreateTag (TagType type)
        {
            if (Tag.Count > 0)
                return Tag.ValueType == type;
            else
                return Enum.IsDefined(typeof(TagType), type) && type != TagType.TAG_END;
        }

        public override async Task<bool> CanPasteIntoNode()
        {
            if (await NbtClipboardController.ContainsDataAsync()) {
                NbtClipboardData data = await NbtClipboardController.CopyFromClipboardAsync();
                if (data == null)
                    return false;

                if (data.Node != null && (data.Node.GetTagType() == Tag.ValueType || Tag.Count == 0))
                    return true;
            }

            return false;
        }

        public override bool CreateNode (TagType type)
        {
            if (!CanCreateTag(type))
                return false;

            if (Tag.Count == 0) {
                Tag.ChangeValueType(type);
            }

            AppendTag(TagDataNode.DefaultTag(type));
            return true;
        }

        public override async Task<bool> PasteNode ()
        {
            if (!await CanPasteIntoNode())
                return false;

            NbtClipboardData clipboard = await NbtClipboardController.CopyFromClipboardAsync();
            if (clipboard == null || clipboard.Node == null)
                return false;

            if (Tag.Count == 0) {
                Tag.ChangeValueType(clipboard.Node.GetTagType());
            }

            AppendTag(clipboard.Node);
            return true;
        }

        public override bool IsOrderedContainer
        {
            get { return true; }
        }

        public override IOrderedTagContainer OrderedTagContainer
        {
            get { return _container; }
        }

        public override int TagCount
        {
            get { return _container.TagCount; }
        }

        public override bool DeleteTag (TagNode tag)
        {
            return _container.DeleteTag(tag);
        }

        public override void Clear ()
        {
            if (TagCount == 0)
                return;

            Nodes.Clear();
            Tag.Clear();

            IsDataModified = true;
        }

        public bool AppendTag (TagNode tag)
        {
            if (tag == null || !CanCreateTag(tag.GetTagType()))
                return false;

            _container.InsertTag(tag, _container.TagCount);
            IsDataModified = true;

            if (IsExpanded) {
                TagDataNode node = TagDataNode.CreateFromTag(tag);
                if (node != null)
                    Nodes.Add(node);
            }

            return true;
        }
    }
}
