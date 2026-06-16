using System;
using System.Text.RegularExpressions;
using Substrate.Core;

namespace NBTModel.Data;

public partial class CubicRegionFile(string path) : RegionFile(path)
{
    private const int SECTOR_BYTES = 256;
    private static readonly Regex NamePattern = MyRegex();
    protected override byte[] EmptySector => new byte[SECTOR_BYTES];

    protected override int SectorBytes => SECTOR_BYTES;

    public override RegionKey parseCoordinatesFromName()
    {
        var match = NamePattern.Match(fileName);
        if (!match.Success) return RegionKey.InvalidRegion;

        var x = Convert.ToInt32(match.Groups[1].Value);
        var z = Convert.ToInt32(match.Groups[3].Value);

        return new RegionKey(x, z);
    }

    [GeneratedRegex(@"r2\.(-?[0-9]+)\.(-?[0-9]+)\.(-?[0-9]+)\.mc[ar]$")]
    private static partial Regex MyRegex();
}