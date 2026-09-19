using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTModel.Data.Nodes;
using NBTModel.Search;

namespace NBTLoupe.Core.TreeNodes;

// This does the *actual* search work of the Advanced Find.
internal class NodeSearcherAdvanced(TreeNode parent, GroupRule rules) : NodeSearcher(parent)
{
    // NBTModel requires a List to save its Matches to, here we define it so we can Operate on it.
    internal List<TagDataNode?> CurrentMatchedTags { get; } = [];

    // This gets NBTModel to check if a TreeNode matches our Rules.
    protected override bool IsMatch(TreeNode node)
    {
        // For this, though, we do need to clean the List, as NBTModel won't do it for us, and we don't want past searches mixed in.
        CurrentMatchedTags.Clear();

        return node.DataNode is TagCompoundDataNode container && rules.Matches(container, CurrentMatchedTags);
    }
}

// TreeRule implementation to be able to interface with Avalonia's TreeView.
internal partial class TreeRule : ObservableObject
{
    // And here's how you create the actual TreeRule!
    internal TreeRule(SearchRule rule, TreeRule? parent = null)
    {
        Rule = rule;
        Parent = parent;
        Title = rule.NodeDisplay;

        // If the Rule isn't a Group, we're done.
        if (rule is not GroupRule group) return;

        // But if it is, then it may have Child Rules. So we make sure to add these into our UI-wise Children Collection. 
        foreach (var child in group.Rules) Children.Add(new TreeRule(child, this));
    }

    // ...it includes its Children, its data (Rule), and its Parent.
    internal ObservableCollection<TreeRule> Children { get; } = [];
    internal SearchRule Rule { get; set; }
    internal TreeRule? Parent { get; }

    // Oh, but all that data is for our fun. Avalonia cares about its Title and its Icon, which is here.
    [ObservableProperty] internal partial string Title { get; set; }

    internal string Icon => Rule switch
    {
        // Here's the list that matches a FluentIcon to each SearchRule Type!
        RootRule => "ArrowDownRight",
        UnionRule => "ShapeUnion",
        IntersectRule => "ShapeIntersect",
        WildcardRule => "TextAsterisk",
        ByteTagRule => "NumberCircle1",
        ShortTagRule => "NumberCircle2",
        IntTagRule => "NumberCircle4",
        LongTagRule => "NumberCircle8",
        FloatTagRule => "DecimalArrowLeft",
        DoubleTagRule => "DecimalArrowRight",
        StringTagRule => "TextT",
        _ => "QuestionCircle"
    };

    // Create an IsExpanded property.
    [ObservableProperty] internal partial bool IsExpanded { get; set; } = true;

    // Oh, and this is how you refresh its Title if you have to.
    internal void RefreshTitle()
    {
        Title = Rule.NodeDisplay;
    }

    // Oh, and this is how you Add a new SearchRule to your Tree!
    internal TreeRule Add(SearchRule rule)
    {
        // We check if we can add Rules to this Parent.
        if (Rule is not GroupRule { CanAddRules: true } groupRule) throw new UnreachableException();

        // We first create the Node for the new Rule...
        var node = new TreeRule(rule, this);

        // ...then we add the new Rule itself to the GroupRule...
        groupRule.Rules.Add(rule);

        // ...then we add its Node (UI-wise) to the GroupRule...
        Children.Add(node);

        // ...then we make sure the Parent IsExpanded (UI-wise), so the user can see the new Rule...
        IsExpanded = true;

        // ...and we return the just created Node.
        return node;
    }

    // Oh, and this is how you Delete a SearchRule from your Tree!
    internal void Delete()
    {
        // We make sure the Rule to be Deleted has a valid Parent, as otherwise we'd be in the Root which can't be Deleted, so we throw if so.
        if (Parent?.Rule is not GroupRule groupRule) throw new UnreachableException();

        // We first delete the Rule itself from the GroupRule...
        groupRule.Rules.Remove(Rule);

        // ...then we delete it UI-wise.
        Parent.Children.Remove(this);
    }
}

// This allows us to reuse a RelayCommand in our Bindings. Convenient!
internal enum MatchGroupKind
{
    All,
    Any
}
