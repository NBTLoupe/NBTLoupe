using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NBTModel.Interop;
using Substrate.Core;

namespace NBTModel.Data.Nodes;

public partial class RegionFileDataNode : DataNode
{
    private static readonly Regex NamePattern = MyRegex();
    private readonly List<RegionKey> _deleteQueue = [];
    private readonly string _path;
    private RegionFile? _region;

    private RegionFileDataNode(string path)
    {
        _path = path;
    }

    protected override NodeCapabilities Capabilities =>
        NodeCapabilities.Search
        | NodeCapabilities.Refresh;

    public override bool HasUnexpandedChildren => !IsExpanded;

    public override bool IsContainerType => true;

    public override string NodePathName => Path.GetFileName(_path);

    public override string NodeDisplay => Path.GetFileName(_path);

    public static RegionFileDataNode TryCreateFrom(string path)
    {
        return new RegionFileDataNode(path);
    }

    public static bool SupportedNamePattern(string path)
    {
        path = Path.GetFileName(path);
        return NamePattern.IsMatch(path);
    }

    protected override void ExpandCore()
    {
        try
        {
            _region ??= new RegionFile(_path);

            for (var x = 0; x < 32; x++)
            for (var z = 0; z < 32; z++)
                if (_region.HasChunk(x, z))
                    Nodes.Add(new RegionChunkDataNode(_region, x, z));
        }
        catch (Exception)
        {
            FormRegistry.MessageBox?.Invoke("Not a valid region file.");
        }
    }

    protected override void ReleaseCore()
    {
        _region?.Close();
        _region = null;
        Nodes.Clear();
    }

    protected override void SaveCore()
    {
        foreach (var key in _deleteQueue.Where(key => _region?.HasChunk(key.X, key.Z) == true))
            _region?.DeleteChunk(key.X, key.Z);

        _deleteQueue.Clear();
    }

    public override bool RefreshNode()
    {
        var expandSet = BuildExpandSet(this);
        Release();
        RestoreExpandSet(this, expandSet);

        return expandSet != null;
    }

    public void QueueDeleteChunk(int rx, int rz)
    {
        var key = new RegionKey(rx, rz);
        if (!_deleteQueue.Contains(key))
            _deleteQueue.Add(key);
    }

    [GeneratedRegex(@"^r\.(-?\d+)\.(-?\d+)\.(mcr|mca)$")]
    private static partial Regex MyRegex();
}
