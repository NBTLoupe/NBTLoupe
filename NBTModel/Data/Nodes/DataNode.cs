using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NBTModel.Utility;
using Substrate.Nbt;

namespace NBTModel.Data.Nodes;

public class DataNode
{
    private bool _childModified;
    private bool _dataModified;

    protected DataNode()
    {
        Nodes = new DataNodeCollection(this, new SnapshotList<DataNode>([]));
    }

    public DataNode? Parent { get; internal set; }

    public DataNode Root => Parent == null ? this : Parent.Root;

    public DataNodeCollection Nodes { get; }

    public bool IsModified => _dataModified || _childModified;

    protected bool IsDataModified
    {
        set
        {
            _dataModified = value;
            CalculateChildModifiedState();
        }
    }

    protected bool IsParentModified
    {
        set => Parent?.IsDataModified = value;
    }

    protected bool IsExpanded { get; private set; }

    public virtual string NodeName => "";

    public string NodePath
    {
        get
        {
            var name = NodePathName;
            if (string.IsNullOrEmpty(name))
                name = "*";

            return Parent != null ? Parent.NodePath + '/' + name : '/' + name;
        }
    }

    public virtual string NodePathName => NodeName;

    public virtual string NodeDisplay => "";

    public virtual bool IsContainerType => false;

    public virtual bool HasUnexpandedChildren => false;

    private void CalculateChildModifiedState()
    {
        _childModified = false;
        foreach (var child in Nodes)
            if (child.IsModified)
                _childModified = true;

        Parent?.CalculateChildModifiedState();
    }

    public void Expand()
    {
        if (IsExpanded) return;
        ExpandCore();
        IsExpanded = true;
    }

    protected virtual void ExpandCore()
    {
    }

    protected void Release()
    {
        foreach (var node in Nodes)
            node.Release();

        ReleaseCore();
        IsExpanded = false;
        IsDataModified = false;
    }

    protected virtual void ReleaseCore()
    {
        Nodes.Clear();
    }

    public void Save()
    {
        foreach (var node in Nodes)
            if (node.IsModified)
                node.Save();

        SaveCore();
        IsDataModified = false;
    }

    protected virtual void SaveCore()
    {
    }

    protected static Dictionary<string, object>? BuildExpandSet(DataNode node)
    {
        if (node is not { IsExpanded: true })
            return null;

        var dict = new Dictionary<string, object>();
        foreach (var child in node.Nodes)
        {
            var childDict = BuildExpandSet(child);
            if (childDict != null) dict[child.NodePathName] = childDict;
        }

        return dict;
    }

    protected static void RestoreExpandSet(DataNode node, Dictionary<string, object>? expandSet)
    {
        if (expandSet == null)
            return;

        node.Expand();

        foreach (var child in node.Nodes)
        {
            if (!expandSet.TryGetValue(child.NodePathName, out var value)) continue;
            var childDict = (Dictionary<string, object>)value;
            RestoreExpandSet(child, childDict);
        }
    }

    #region Node Capabilities

    protected virtual NodeCapabilities Capabilities => NodeCapabilities.None;

    public virtual bool CanRenameNode => (Capabilities & NodeCapabilities.Rename) != NodeCapabilities.None;

    public virtual bool CanEditNode => (Capabilities & NodeCapabilities.Edit) != NodeCapabilities.None;

    public bool CanDeleteNode => (Capabilities & NodeCapabilities.Delete) != NodeCapabilities.None;

    public bool CanCopyNode => (Capabilities & NodeCapabilities.Copy) != NodeCapabilities.None;

    public bool CanCutNode => (Capabilities & NodeCapabilities.Cut) != NodeCapabilities.None;

    public virtual Task<bool> CanPasteIntoNode()
    {
        try
        {
            return Task.FromResult((Capabilities & NodeCapabilities.PasteInto) != NodeCapabilities.None);
        }
        catch (Exception exception)
        {
            return Task.FromException<bool>(exception);
        }
    }

    public bool CanSearchNode => (Capabilities & NodeCapabilities.Search) != NodeCapabilities.None;

    public bool CanReoderNode => (Capabilities & NodeCapabilities.Reorder) != NodeCapabilities.None;

    public bool CanRefreshNode => (Capabilities & NodeCapabilities.Refresh) != NodeCapabilities.None;

    public virtual bool CanMoveNodeUp => false;

    public virtual bool CanMoveNodeDown => false;

    public virtual bool CanCreateTag(TagType type)
    {
        return false;
    }

    #endregion

    #region Operations

    public virtual bool CreateNode(TagType type)
    {
        return false;
    }

    public virtual bool RenameNode()
    {
        return false;
    }

    public virtual bool EditNode()
    {
        return false;
    }

    public virtual bool DeleteNode()
    {
        return false;
    }

    public virtual Task<bool> CopyNode()
    {
        return Task.FromResult(false);
    }

    public virtual Task<bool> CutNode()
    {
        return Task.FromResult(false);
    }

    public virtual Task<bool> PasteNode()
    {
        return Task.FromResult(false);
    }

    public virtual bool ChangeRelativePosition(int offset)
    {
        return false;
    }

    public virtual bool RefreshNode()
    {
        return false;
    }

    #endregion
}