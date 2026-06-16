using System;
using System.Collections.Generic;
using NBTModel.Data.Nodes;

namespace NBTModel.Data;

public delegate bool NamePatternTestFunc(string path);

public delegate DataNode NodeCreateFunc(string path);

public class FileTypeRecord
{
    public required NamePatternTestFunc NamePatternTest { get; init; }

    public required NodeCreateFunc NodeCreate { get; init; }
}

public static class FileTypeRegistry
{
    private static readonly Dictionary<Type, FileTypeRecord> Registry = new();

    static FileTypeRegistry()
    {
        try
        {
            Register<NbtFileDataNode>(new FileTypeRecord
            {
                NamePatternTest = NbtFileDataNode.SupportedNamePattern,
                NodeCreate = NbtFileDataNode.TryCreateFrom
            });

            Register<RegionFileDataNode>(new FileTypeRecord
            {
                NamePatternTest = RegionFileDataNode.SupportedNamePattern,
                NodeCreate = RegionFileDataNode.TryCreateFrom
            });

            Register<CubicRegionDataNode>(new FileTypeRecord
            {
                NamePatternTest = CubicRegionDataNode.SupportedNamePattern,
                NodeCreate = CubicRegionDataNode.TryCreateFrom
            });
        }
        catch (Exception e)
        {
            Environment.FailFast("NBTModel failed to initialize.", e);
        }
    }

    public static IEnumerable<KeyValuePair<Type, FileTypeRecord>> RegisteredTypes => Registry;

    private static void Register(Type type, FileTypeRecord record)
    {
        Registry[type] = record;
    }

    private static void Register<T>(FileTypeRecord record)
    {
        Register(typeof(T), record);
    }
}
