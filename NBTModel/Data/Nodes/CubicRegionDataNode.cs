using System.IO;
using System.Text.RegularExpressions;
using NBTModel.Interop;

namespace NBTModel.Data.Nodes;

public partial class CubicRegionDataNode : DataNode
{
    private static readonly Regex NamePattern = MyRegex();
    private readonly string _path;
    private CubicRegionFile? _region;

    private CubicRegionDataNode(string path)
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

    public static CubicRegionDataNode TryCreateFrom(string path)
    {
        return new CubicRegionDataNode(path);
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
            _region ??= new CubicRegionFile(_path);

            for (var x = 0; x < 32; x++)
            for (var z = 0; z < 32; z++)
                if (_region.HasChunk(x, z))
                    Nodes.Add(new RegionChunkDataNode(_region, x, z));
        }
        catch
        {
            FormRegistry.MessageBox?.Invoke("Not a valid cubic region file.");
        }
    }

    protected override void ReleaseCore()
    {
        _region?.Close();
        _region = null;
        Nodes.Clear();
    }

    public override bool RefreshNode()
    {
        var expandSet = BuildExpandSet(this);
        Release();
        RestoreExpandSet(this, expandSet);

        return expandSet != null;
    }

    [GeneratedRegex(@"^r2(\.-?\d+){3}\.(mcr|mca)$")]
    private static partial Regex MyRegex();
}
