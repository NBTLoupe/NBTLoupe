using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NBTModel.Interop;
using Substrate.Core;
using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public partial class NbtFileDataNode : DataNode, IMetaTagContainer
{
    private static readonly Regex NamePattern = MyRegex();
    private readonly CompressionType _compressionType;
    private readonly string _path;

    private CompoundTagContainer _container;
    private NbtTree? _tree;

    private NbtFileDataNode(string path, CompressionType compressionType)
    {
        _path = path;
        _compressionType = compressionType;
        _container = new CompoundTagContainer(new TagNodeCompound());
    }

    public string? TreeName => _tree?.Name;

    protected override NodeCapabilities Capabilities =>
        NodeCapabilities.CreateTag
        | NodeCapabilities.PasteInto
        | NodeCapabilities.Search
        | NodeCapabilities.Refresh
        | NodeCapabilities.Rename;

    public override string NodeName => Path.GetFileName(_path);

    public override string NodePathName => Path.GetFileName(_path);

    public override string NodeDisplay
    {
        get
        {
            if (_tree is not { Root: not null }) return NodeName;
            if (!string.IsNullOrEmpty(_tree.Name))
                return NodeName + " [" + _tree.Name + ": " + _tree.Root.Count + " entries]";
            return NodeName + " [" + _tree.Root.Count + " entries]";
        }
    }

    public override bool HasUnexpandedChildren => !IsExpanded;

    public override bool IsContainerType => true;

    public override bool CanRenameNode => _tree != null;

    public bool IsNamedContainer => true;

    public bool IsOrderedContainer => false;

    public INamedTagContainer NamedTagContainer => _container;

    public IOrderedTagContainer? OrderedTagContainer => null;

    public int TagCount => _container.TagCount;

    public bool DeleteTag(TagNode tag)
    {
        return _container.DeleteTag(tag);
    }

    public static NbtFileDataNode TryCreateFrom(string path)
    {
        return (TryCreateFrom(path, CompressionType.GZip) ?? TryCreateFrom(path, CompressionType.None)) ??
               throw new InvalidOperationException();
    }

    private static NbtFileDataNode? TryCreateFrom(string path, CompressionType compressionType)
    {
        try
        {
            var file = new NBTFile(path);
            var tree = new NbtTree();
            tree.ReadFrom(file.GetDataInputStream(compressionType));

            return tree.Root == null ? null : new NbtFileDataNode(path, compressionType);
        }
        catch
        {
            return null;
        }
    }

    public static bool SupportedNamePattern(string path)
    {
        path = Path.GetFileName(path);
        return NamePattern.IsMatch(path);
    }

    protected override void ExpandCore()
    {
        if (_tree == null)
        {
            var file = new NBTFile(_path);
            _tree = new NbtTree();
            _tree.ReadFrom(file.GetDataInputStream(_compressionType));

            if (_tree.Root != null) _container = new CompoundTagContainer(_tree.Root);
        }

        var list = new SortedList<TagKey, TagNode>();
        if (_tree.Root == null) throw new NullReferenceException();
        foreach (var item in _tree.Root) list.Add(new TagKey(item.Key, item.Value.GetTagType()), item.Value);

        foreach (var tag in list.Values)
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
        var file = new NBTFile(_path);
        using var str = file.GetDataOutputStream(_compressionType);
        _tree?.WriteTo(str);
    }

    public override bool RefreshNode()
    {
        var expandSet = BuildExpandSet(this);
        Release();
        RestoreExpandSet(this, expandSet);

        return expandSet != null;
    }

    public override bool RenameNode()
    {
        if (!CanRenameNode || FormRegistry.RenameTag == null) return false;
        var data = new RestrictedStringFormData(_tree?.Name ?? "");

        if (!FormRegistry.RenameTag(data)) return false;
        if (_tree?.Name == data.Value) return false;
        _tree?.Name = data.Value;
        IsDataModified = true;
        return true;
    }

    public override bool CanCreateTag(TagType type)
    {
        return _tree is { Root: not null } && Enum.IsDefined(type) && type != TagType.TAG_END;
    }

    public override async Task<bool> CanPasteIntoNode()
    {
        return _tree is { Root: not null } && await NbtClipboardController.ContainsDataAsync();
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

    private void AddTag(TagNode tag, string name)
    {
        _container.AddTag(tag, name);
        IsDataModified = true;

        if (!IsExpanded) return;
        var node = TagDataNode.CreateFromTag(tag);
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

    [GeneratedRegex(@"\.(dat|nbt|schematic|dat_mcr|dat_old|bpt|rc)$")]
    private static partial Regex MyRegex();
}
