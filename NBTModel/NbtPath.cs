using System.Collections;
using System.Collections.Generic;
using System.IO;
using NBTModel.Data.Nodes;

namespace NBTModel;

public class NbtPathEnumerator : IEnumerable<DataNode>
{
    private readonly List<string> _pathParts;
    private readonly string _pathRoot;

    public NbtPathEnumerator(string path)
    {
        _pathRoot = Path.GetPathRoot(path) ?? string.Empty;
        _pathParts = new List<string>(path[_pathRoot.Length..].Split('/', '\\'));

        if (string.IsNullOrEmpty(_pathRoot))
            _pathRoot = Directory.GetCurrentDirectory();
    }

    public IEnumerator<DataNode> GetEnumerator()
    {
        DataNode dataNode = new DirectoryDataNode(_pathRoot);
        dataNode.Expand();

        foreach (var childNode in EnumerateNodes(dataNode, _pathParts))
            yield return childNode;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static IEnumerable<DataNode> EnumerateNodes(DataNode containerNode, List<string> nextLevels)
    {
        containerNode.Expand();
        if (nextLevels.Count == 0)
        {
            yield return containerNode;
            yield break;
        }

        if (containerNode.Nodes.Count == 0)
            yield break;

        var part = nextLevels[0];
        var remainingLevels = nextLevels.GetRange(1, nextLevels.Count - 1);

        switch (part)
        {
            case "*":
            {
                foreach (var childNode in containerNode.Nodes)
                foreach (var grandChildNode in EnumerateNodes(childNode, remainingLevels))
                    yield return grandChildNode;

                break;
            }
            case "**":
            {
                foreach (var childNode in containerNode.Nodes)
                {
                    foreach (var grandChildNode in EnumerateNodes(childNode, remainingLevels))
                        yield return grandChildNode;

                    foreach (var grandChildNode in EnumerateNodes(childNode, nextLevels))
                        yield return grandChildNode;
                }

                break;
            }
            default:
            {
                foreach (var childNode in containerNode.Nodes)
                {
                    if (childNode.NodePathName != part) continue;
                    foreach (var grandChildNode in EnumerateNodes(childNode, remainingLevels))
                        yield return grandChildNode;
                }

                break;
            }
        }
    }
}
