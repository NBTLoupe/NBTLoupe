using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTModel.Data.Nodes;

namespace NBTLoupe.Core.TreeNodes;

// This does the *actual* search work of the Basic Find.
internal partial class NodeBasicSearcher(TreeNode parent, string? name, string? value) : ObservableObject
{
    // First we store the Name and/or Value inserted, as we use it in the Dialog. 
    internal readonly string? Name = name;
    internal readonly string? Value = value;

    // We also cache the found ones as we go, which lets us go backwards.
    private readonly List<TreeNode> _alreadyFound = [];

    // We need to snapshot our toVisit as we keep visiting in case our cache ever gets partially invalidated.
    private readonly List<TreeNode[]> _toVisitSnapshot = [];

    // We create a Stack of all the Nodes we have to visit, starting from the parent.
    private Stack<TreeNode> _toVisit = new([parent]);

    // This stores where we are when transversing our search.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentMatch))]
    private partial int Index { get; set; } = -1;

    // This gives us a cute number to use in the UI.
    internal int CurrentMatch => Index + 1;

    // And this gives use another cute number to use in the UI, although only once we reached the end.
    [ObservableProperty] internal partial int? TotalMatches { get; private set; }

    // Then, every time we need to Find the Next result...
    internal async Task<TreeNode?> FindNextAsync()
    {
        // If we already found this result, but moved behind it...
        if (Index + 1 < _alreadyFound.Count)
        {
            // ...we retrieve it from the cache...
            var potentialFind = Index + 1;
            var node = _alreadyFound[potentialFind];

            // ...then check if it still exists and is a match...
            if (IsStillInTree(node, parent) && IsMatch(node))
            {
                // ...and if so, we immediately return it.
                Index = potentialFind;
                return node;
            }

            // If it's invalidated for some reason, our cache also is. So we reset our TotalMatches counter...
            TotalMatches = null;

            // ...we remove every item starting from it from the cache...
            _alreadyFound.RemoveRange(potentialFind, _alreadyFound.Count - potentialFind);

            // ...and, unless the cache got invalidated at the root, we restore our snapshot... 
            var snapshot = potentialFind == 0 ? [parent] : _toVisitSnapshot[potentialFind - 1];
            _toVisit = new Stack<TreeNode>(snapshot.Reverse());

            // ...and remove from it what we just restored. This allows us to start rebuilding the cache from the point it got invalidated.
            _toVisitSnapshot.RemoveRange(potentialFind, _toVisitSnapshot.Count - potentialFind);
        }

        // ...if we still have something To Visit...
        while (_toVisit.TryPop(out var node))
        {
            // ...and it still exists...
            if (!IsStillInTree(node, parent)) continue;

            // ...we make sure it is lazy-loaded...
            await node.LazyLoadAsync();

            // ...and if it has any SubNodes...
            if (node.SubNodes is not null)
                // ...we loop through them, although in reverse to follow a logical visual order... 
                foreach (var child in node.SubNodes.Reverse())
                    // ...and we add them into our To Visit Stack, so we can continue the loop through them if needed.
                    _toVisit.Push(child);

            // Once we finish that, if this current Node is a valid result...
            if (!IsMatch(node)) continue;

            // ...we cache it...
            _alreadyFound.Add(node);

            // ...snapshot our To Visit Stack, in case it ever becomes invalid...
            _toVisitSnapshot.Add([.. _toVisit]);

            // ...and return it.
            Index = _alreadyFound.Count - 1;
            return node;
        }

        // If we emptied the To Visit Stack and haven't found anything, we just return null.
        TotalMatches = _alreadyFound.Count;
        return null;
    }

    // Then, every time we need to Find the Previous result...
    internal TreeNode? FindPrevious()
    {
        // ...we start looping... 
        while (Index > 0)
        {
            // ...and going backwards once every time, and retrieving that item from the cache...
            Index--;
            var potentialFind = _alreadyFound[Index];

            // ...and if it still exists and is a match, we immediately return it.
            if (IsStillInTree(potentialFind, parent) && IsMatch(potentialFind))
                return potentialFind;

            // If not, then that means we'll be looping backwards once more. This wouldn't happen unless the TreeNode (and this the cache) is invalidated. So we reset our TotalMatches counter.
            TotalMatches = null;
        }

        // If we finished the loop and haven't found anything, we just return null.
        return null;
    }

    // This helps us check if a specific TreeNode is still a descendent of the given Parent.
    private static bool IsStillInTree(TreeNode node, TreeNode parent)
    {
        // We first save the current node. 
        var current = node;

        // Then we start looping upwards, until we reach our given Parent.
        while (current != parent)
        {
            // If either Parent is null, has no children, or none of its children is ours, then our specific TreeNode is gone.
            // So, we return false and stop climbing.
            if (current.Parent?.SubNodes?.Contains(current) != true) return false;

            // But if our saved child is still there, we keep climbing.
            current = current.Parent;
        }

        // If we finish climbing, and we never encountered a broken Parent, our TreeNode is still valid.
        return true;
    }

    // This checks both of our Basic Search conditions with a given TreeNode.
    private bool IsMatch(TreeNode node)
    {
        return node.DataNode is TagDataNode &&
               (Name is null ||
                node.DataNode.NodeName.Contains(Name,
                    StringComparison.InvariantCultureIgnoreCase)) &&
               (Value is null ||
                node.DataNode.NodeDisplay.Contains(Value,
                    StringComparison.InvariantCultureIgnoreCase));
    }
}
