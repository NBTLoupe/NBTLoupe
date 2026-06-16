using System;
using System.Collections;
using System.Collections.Generic;
using NBTModel.Data.Nodes;
using NBTModel.Utility;

namespace NBTModel.Data;

public class DataNodeCollection : IList<DataNode>
{
    private readonly SnapshotList<DataNode> _nodes;
    private readonly DataNode _parent;

    internal DataNodeCollection(DataNode parent, SnapshotList<DataNode> nodes)
    {
        _parent = parent;
        _nodes = nodes;
    }

    private int ChangeCount { get; set; }

    public int IndexOf(DataNode item)
    {
        return _nodes.IndexOf(item);
    }

    public void Insert(int index, DataNode item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Parent != null)
            throw new ArgumentException("The item is already assigned to another DataNode.");

        item.Parent = _parent;

        _nodes.Insert(index, item);
        ChangeCount++;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _nodes.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var node = _nodes[index];
        node.Parent = null;

        _nodes.RemoveAt(index);
        ChangeCount++;
    }

    DataNode IList<DataNode>.this[int index]
    {
        get => _nodes[index];
        set
        {
            if (index < 0 || index > _nodes.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            ArgumentNullException.ThrowIfNull(value);
            if (value.Parent != null)
                throw new ArgumentException("The item is already assigned to another DataNode.");

            _nodes[index].Parent = null;
            _nodes[index] = value;
            _nodes[index].Parent = _parent;
            ChangeCount++;
        }
    }

    public void Add(DataNode item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Parent != null)
            throw new ArgumentException("The item is already assigned to another DataNode.");

        item.Parent = _parent;

        _nodes.Add(item);
        ChangeCount++;
    }

    public void Clear()
    {
        foreach (var node in _nodes)
            node.Parent = null;

        _nodes.Clear();
        ChangeCount++;
    }

    public bool Contains(DataNode item)
    {
        return _nodes.Contains(item);
    }

    public void CopyTo(DataNode[] array, int arrayIndex)
    {
        _nodes.CopyTo(array, arrayIndex);
    }

    public int Count => _nodes.Count;

    bool ICollection<DataNode>.IsReadOnly => (_nodes as IList<DataNode>).IsReadOnly;

    public bool Remove(DataNode item)
    {
        if (_nodes.Contains(item)) item.Parent = null;

        ChangeCount++;

        return _nodes.Remove(item);
    }

    public IEnumerator<DataNode> GetEnumerator()
    {
        return _nodes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _nodes.GetEnumerator();
    }
}
