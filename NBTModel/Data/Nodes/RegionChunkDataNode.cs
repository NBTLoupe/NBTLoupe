using System;
using Substrate.Core;
using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class RegionChunkDataNode(RegionFile regionFile, int x, int z) : DataNode, IMetaTagContainer
{
    private CompoundTagContainer _container = new(new TagNodeCompound());
    private NbtTree? _tree;

    private RegionFileDataNode? RegionParent => (RegionFileDataNode?)Parent;

    protected override NodeCapabilities Capabilities =>
        NodeCapabilities.CreateTag
        | NodeCapabilities.PasteInto
        | NodeCapabilities.Search
        | NodeCapabilities.Delete;

    public override bool HasUnexpandedChildren => !IsExpanded;

    public override string NodePathName => x + "." + z;

    public override string NodeDisplay
    {
        get
        {
            var key = regionFile.parseCoordinatesFromName();
            var absChunk = "";
            if (key != RegionKey.InvalidRegion)
                absChunk = "        in world at (" + (key.X * 32 + x) + ", " + (key.Z * 32 + z) + ")";

            return "Chunk [" + x + ", " + z + "]" + absChunk;
        }
    }

    public override bool IsContainerType => true;

    public bool IsNamedContainer => true;

    public bool IsOrderedContainer => false;

    public INamedTagContainer NamedTagContainer => _container;

    public IOrderedTagContainer? OrderedTagContainer => null;

    public int TagCount => _container.TagCount;

    public bool DeleteTag(TagNode tag)
    {
        return _container.DeleteTag(tag);
    }

    protected override void ExpandCore()
    {
        if (_tree == null)
        {
            _tree = new NbtTree();
            _tree.ReadFrom(regionFile.GetChunkDataInputStream(x, z));

            if (_tree.Root != null)
                _container = new CompoundTagContainer(_tree.Root);
        }

        if (_tree.Root == null) throw new NullReferenceException();
        foreach (var tag in _tree.Root.Values)
        {
            var node = TagDataNode.CreateFromTag(tag);
            if (node != null)
                Nodes.Add(node);
        }
    }

    protected override void ReleaseCore()
    {
        _tree = null;
        Nodes.Clear();
    }

    protected override void SaveCore()
    {
        using var str = regionFile.GetChunkDataOutputStream(x, z);
        _tree?.WriteTo(str);
    }

    public override bool DeleteNode()
    {
        if (!CanDeleteNode || !regionFile.HasChunk(x, z)) return false;
        RegionParent?.QueueDeleteChunk(x, z);
        IsParentModified = true;
        return Parent?.Nodes.Remove(this) == true;
    }
}
