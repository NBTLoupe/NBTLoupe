using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NBTModel.Utility;
// NaturalComparer implementation by Justin.Jones
// Licensed under The Code Project Open License (CPOL) (http://www.codeproject.com/info/cpol10.aspx)

public partial class NaturalComparer : Comparer<string>, IDisposable
{
    private readonly Dictionary<string, string[]> _table = new();

    public void Dispose()
    {
        _table.Clear();
        GC.SuppressFinalize(this);
    }

    public override int Compare(string? x, string? y)
    {
        if (x == y) return 0;

        if (!_table.TryGetValue(x ?? throw new ArgumentNullException(nameof(x)), out var x1))
        {
            x1 = MyRegex().Split(x.Replace(" ", ""));
            _table.Add(x, x1);
        }

        if (!_table.TryGetValue(y ?? throw new ArgumentNullException(nameof(y)), out var y1))
        {
            y1 = MyRegex().Split(y.Replace(" ", ""));
            _table.Add(y, y1);
        }

        for (var i = 0; i < x1.Length && i < y1.Length; i++)
            if (x1[i] != y1[i])
                return PartCompare(x1[i], y1[i]);

        if (y1.Length > x1.Length) return 1;

        if (x1.Length > y1.Length) return -1;

        return 0;
    }

    private static int PartCompare(string left, string right)
    {
        if (!int.TryParse(left, out var x) || !int.TryParse(right, out var y))
            return string.Compare(left, right, StringComparison.Ordinal);

        return x.CompareTo(y);
    }

    [GeneratedRegex("(-?[0-9]+)")]
    private static partial Regex MyRegex();
}