using System;
using System.IO;

namespace NBTModel.Data.Nodes;

public class DirectoryDataNode(string path) : DataNode
{
    protected override NodeCapabilities Capabilities =>
        NodeCapabilities.Search
        | NodeCapabilities.Refresh;

    public string NodeDirPath => path;

    public override string NodePathName
    {
        get
        {
            var path1 = path.EndsWith('/') || path.EndsWith('\\') ? path : path + '/';

            var name = Path.GetDirectoryName(path1) ?? path1[..^1];
            var sepIndex = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));

            return sepIndex > 0 ? name[(sepIndex + 1)..] : name;
        }
    }

    public override string NodeDisplay => Path.GetFileName(path);

    public override bool HasUnexpandedChildren => !IsExpanded;

    public override bool IsContainerType => true;

    protected override void ExpandCore()
    {
        foreach (var dirpath in Directory.GetDirectories(path)) Nodes.Add(new DirectoryDataNode(dirpath));

        foreach (var filepath in Directory.GetFiles(path))
        {
            DataNode? node = null;
            foreach (var item in FileTypeRegistry.RegisteredTypes)
                if (item.Value.NamePatternTest(filepath))
                    node = item.Value.NodeCreate(filepath);

            if (node != null)
                Nodes.Add(node);
        }
    }

    protected override void ReleaseCore()
    {
        Nodes.Clear();
    }

    public override bool RefreshNode()
    {
        var expandSet = BuildExpandSet(this);
        Release();
        RestoreExpandSet(this, expandSet);

        return expandSet != null;
    }
}