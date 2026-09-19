using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NBTLoupe.Core.TreeNodes;
using NBTLoupe.ViewModels.Main;
using NBTModel.Data.Nodes;
using NBTModel.Search;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the Advanced Find and Replace Dialog!
internal partial class FindAdvancedDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal FindAdvancedDialogViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        // Because SelectedReplacementTags is a Collection, we require this EventHandler for our RelayCommands to notice any change.
        SelectedReplacementTags.CollectionChanged += (_, _) =>
        {
            SingleSelectedReplacementTag = SelectedReplacementTags.Count == 1 ? SelectedReplacementTags[0] : null;
        };

        // We create a proper TreeNode with a fancy Compound Tag as its root. This lets us reuse pre-existing TreeNode infrastructure for the Replacement Tags.
        TreeNode.ExpandNode([
                new TagCompoundDataNode(new TagNodeCompound())
            ],
            ReplacementTags,
            mainViewModel.WithBlock,
            factory => mainViewModel.OpenDialogAsync(factory(mainViewModel)),
            null,
            true);

        // Then we auto-select the Rules Root...
        SelectedRuleNode = TreeRules[0];

        // ...and the ReplacementTags Root.
        SelectedReplacementTags.Add(ReplacementTags[0]);
    }

    // This stores our TreeNode implementation.
    internal ObservableCollection<TreeNode> ReplacementTags { get; set; } = [];
    internal ObservableCollection<TreeNode> SelectedReplacementTags { get; set; } = [];

    // We take the singular TreeNode only once and reuse it everywhere.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    [NotifyPropertyChangedFor(nameof(CanSwitchMode))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditTagCommand))]
    internal partial TreeNode? SingleSelectedReplacementTag { get; private set; }

    // This stores our TreeRule implementation.
    private TreeRule RootRule { get; } = new(new RootRule());
    internal ObservableCollection<TreeRule> TreeRules => [RootRule];

    // We take the singular RuleNode only once and reuse it everywhere.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    [NotifyPropertyChangedFor(nameof(CanSwitchMode))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddTagRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddMatchGroupCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRuleCommand))]
    internal partial TreeRule? SelectedRuleNode { get; set; }

    // Here's all the fields we bind to in the XAML...
    // The UI locker...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    internal partial bool InProgress { get; private set; }

    // The Delete Matched Tags CheckBox...
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReplaceAllCommand))]
    internal partial bool DeleteMatchedTags { get; set; }

    // The TabStrip locker...
    internal bool CanSwitchMode => RootRule.Children.Count < 1 &&
                                   ReplacementTags[0].SubNodes?.Count(node => !node.IsPlaceholder) is null or 0;

    // This lets all our Advanced stuff fit by allowing our Dialog to be wider!
    protected override bool IsWide => true;

    // Not really magic, but just a hacky way to be able to show a new Dialog if we don't find anything. 
    internal bool FoundMatch { get; private set; }

    // And here's where our Validation magic happens!
    // Only enable the OK button if:
    // - There isn't a search currently In Progress.
    // - There is at least one Rule set.
    internal override bool IsOkEnabled => !InProgress && RootRule.Rule is GroupRule { Rules.Count: > 0 };

    // This gives the OK button tailor-made text!
    internal override string OkText => "Next...";

    // This allows us to have a special separate buttons for Replace All!
    internal override IReadOnlyList<DialogButton> SpecialButtons => [new("Replace All", ReplaceAllCommand)];

    // This allows us to switch between our supported Nested Dialog Kinds!
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NestedDialogContext))]
    [NotifyPropertyChangedFor(nameof(IsNestedDialogOpen))]
    private partial NestedDialogKinds NestedDialogKind { get; set; }

    // This allows us to pass the new RuleType to our Edit Tag Rule Dialog. 
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditRuleCommand))]
    internal partial TagType? NewRuleTagType { get; private set; }

    // This allows us to pass the new TagType to our Add Tag Dialog.
    [ObservableProperty] internal partial TagType? NewTagType { get; private set; }

    // Then we override our NestedDialogContext so we can Bind to it!
    internal sealed override DialogHostViewModel? NestedDialogContext { get; set; }

    // And here's how we show our Nested Dialog!
    internal override bool IsNestedDialogOpen => NestedDialogKind is not NestedDialogKinds.None;

    // And here's how we let our Nested Dialog close itself!
    protected override Func<Task> CloseNestedDialog => () =>
    {
        NestedDialogKind = NestedDialogKinds.None;
        return Task.CompletedTask;
    };

    // This makes sure the Replace RelayCommands are only enabled when there are Rules set; and there either are ReplacementTags set, or the user chose to just Delete the Matches.
    internal bool CanReplace => RootRule.Rule is GroupRule { Rules.Count: > 0 } &&
                                (ReplacementTags[0].SubNodes?.Count(node => !node.IsPlaceholder) > 0 ||
                                 DeleteMatchedTags);

    // This makes sure the AddRule RelayCommand is only enabled when the selected Rule can have rules Added to it.
    private bool CanAddRule => SelectedRuleNode?.Rule.CanAddRules ?? false;

    // This makes sure the EditRule RelayCommand is only enabled when a non-GroupRule Rule is selected, as these are the only Editable ones.
    private bool CanEditRule => SelectedRuleNode?.Rule is { CanAddRules: false };

    // This makes sure the DeleteRule RelayCommand is only enabled when the selected Rule has a valid Parent, as otherwise we'd be in the Root which can't be Deleted.
    internal bool CanDeleteRule => SelectedRuleNode?.Parent is not null;

    // This makes sure the EditRule RelayCommand is only enabled when the selected Tag is Editable.
    internal bool CanEditTag => SingleSelectedReplacementTag?.DataNode.CanEditNode ?? false;

    // This makes sure the DeleteTag RelayCommand is only enabled when the selected Tag isn't the Root Compound, and can be Deleted by NBTModel.
    private bool CanDeleteTag => (!SingleSelectedReplacementTag?.IsReplacementTagRoot ?? true) &&
                                 (SingleSelectedReplacementTag?.DataNode.CanDeleteNode ?? false);

    // This makes sure the AddTag RelayCommand is only enabled when the selected Tag can have a Child of the selected TagType.
    private bool CanAddTag(TagType tagType)
    {
        return SingleSelectedReplacementTag?.DataNode.CanCreateTag(tagType) ?? false;
    }

    // This tells the MainViewModel to display/hide the ProgressBar when we change our internal InProgress value.
    partial void OnInProgressChanged(bool value)
    {
        MainViewModel.IsDialogProgressing = value;
        DialogCancelCommand.NotifyCanExecuteChanged();
    }

    // This sets our NestedDialogContext only when the NestedDialogKind changes. We do this so we don't create a new ViewModel on every Bind, which would break everything.
    partial void OnNestedDialogKindChanged(NestedDialogKinds value)
    {
        // Oh, and we use an Enum for this to make it very easy to choose which Nested Dialog to open...
        NestedDialogContext = value switch
        {
            NestedDialogKinds.EditTagRule => new EditTagRuleDialogViewModel(MainViewModel, this),
            NestedDialogKinds.AddReplacementTag => new AddTagDialogViewModel(MainViewModel, parent: this),
            NestedDialogKinds.EditReplacementTag => new EditTagDialogViewModel(MainViewModel, false, this),
            _ => null
        };

        // ...or if to close.
        if (value is NestedDialogKinds.None) NewRuleTagType = null;
    }

    // This one is executed when the user chooses to switch Find Modes.
    [RelayCommand]
    private Task<bool> DialogSwitchModes()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            await MainViewModel.FindBasicCommand.ExecuteAsync(null);
            CompletionSource.TrySetResult(false);
        });
    }

    // This one is executed when the user double-clicks a supported item.
    [RelayCommand]
    private Task<bool> DoubleTappedItem(bool isReplacementTag)
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            // If the user double-clicks a ReplacementTag...
            if (isReplacementTag)
            {
                // ...we try to open the EditTag Dialog. 
                if (EditTagCommand.CanExecute(null)) await EditTagCommand.ExecuteAsync(null);
            }
            // But if the user double-clicks a Rule...
            else if (EditRuleCommand.CanExecute(null))
            {
                // ...we try to open the EditRule Dialog.
                await EditRuleCommand.ExecuteAsync(null);
            }
        });
    }

    // This one is executed when the user chooses to execute their Replace operation in All targets at once.
    [RelayCommand(CanExecute = nameof(CanReplace))]
    private Task<bool> ReplaceAll()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            // First we check that there's a SelectedTreeNode, and that our RootRule is defined. This should always be the case, so we throw if not.
            if (MainViewModel.SingleSelectedTreeNode is null || RootRule.Rule is not GroupRule groupRule)
                throw new UnreachableException();

            // We block the UI to prevent the user from doing anything while we process the search.
            InProgress = true;

            // And we create our NodeSearcherAdvanced.
            var find = new NodeSearcherAdvanced(MainViewModel.SingleSelectedTreeNode, groupRule);

            // And, while we can keep finding a new Node...
            while (await find.FindNextAsync() is { } node)
            {
                // We set FoundMatch to true, preventing the "No matching tags were found." dialog from showing.
                FoundMatch = true;

                // And we immediately replace that found Node with our ReplacementTags.
                await Replace(node, find.CurrentMatchedTags);
            }

            // TODO: Tell the user, but not close the Dialog. This is so, also TODO, we can have breadcrumb navigation.
            CompletionSource.TrySetResult(true);
        });
    }

    // This one is executed when the user chooses to Edit a Rule.
    [RelayCommand(CanExecute = nameof(CanEditRule))]
    private Task<bool> EditRule()
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            // We set NestedDialogKind to its corresponding value, opening the Dialog.
            NestedDialogKind = NestedDialogKinds.EditTagRule;

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Delete a Rule.
    [RelayCommand(CanExecute = nameof(CanDeleteRule))]
    private Task<bool> DeleteRule()
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            // We isolate the Parent because its Child is going to be Deleted.
            var parent = SelectedRuleNode?.Parent ?? throw new UnreachableException();

            // We save the current index.
            var index = parent.Children.IndexOf(SelectedRuleNode);

            // We do the actual Deleting.
            SelectedRuleNode.Delete();

            // And re-select a valid item, using the index as a waypoint.
            SelectedRuleNode = parent.Children.Count > 0
                ? parent.Children.ElementAtOrDefault(index - 1) ?? parent.Children.LastOrDefault()
                : parent;

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Add a Match Group (All/Any).
    [RelayCommand(CanExecute = nameof(CanAddRule))]
    private Task<bool> AddMatchGroup(MatchGroupKind matchGroupKind)
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            // We check if there's an actual Rule selected, and throw if not.
            if (SelectedRuleNode is null) throw new UnreachableException();

            // And we just add the Match Group to the selected Parent, then immediately select the new Group.
            SelectedRuleNode = SelectedRuleNode.Add(matchGroupKind switch
            {
                MatchGroupKind.All => new IntersectRule(),
                MatchGroupKind.Any => new UnionRule(),
                _ => throw new UnreachableException()
            });

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Add a Rule.
    [RelayCommand(CanExecute = nameof(CanAddRule))]
    private Task<bool> AddTagRule(TagType? tagType)
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            // We set NewRuleType to the selected RuleKind, allowing the Dialog to read it.
            NewRuleTagType = tagType;

            // And we set NestedDialogKind to its corresponding value, opening the Dialog.
            NestedDialogKind = NestedDialogKinds.EditTagRule;

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Add a Tag.
    [RelayCommand(CanExecute = nameof(CanAddTag))]
    private Task<bool> AddTag(TagType tagType)
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            // If inside a TAG_LIST, we just bypass the Dialog altogether (because it can't have a Name anyway).
            if ((SingleSelectedReplacementTag?.DataNode as TagDataNode)?.Tag.GetTagType() == TagType.TAG_LIST)
            {
                NewTagType = tagType;
                await new AddTagDialogViewModel(MainViewModel, parent: this).ExecuteAsync();

                return;
            }

            // Show the Dialog itself.
            NewTagType = tagType;
            NestedDialogKind = NestedDialogKinds.AddReplacementTag;
        });
    }

    // This one is executed when the user chooses to Edit a Tag.
    [RelayCommand(CanExecute = nameof(CanEditTag))]
    private Task<bool> EditTag()
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            // We set NestedDialogKind to its corresponding value, opening the Dialog.
            NestedDialogKind = NestedDialogKinds.EditReplacementTag;

            return Task.CompletedTask;
        });
    }

    // This one is executed when the user chooses to Delete a Tag.
    [RelayCommand(CanExecute = nameof(CanDeleteTag))]
    private Task<bool> DeleteTag()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            // We check if we have a Parent, as otherwise we are trying to delete the Root Compound, which should never happen.
            var parent = SingleSelectedReplacementTag?.Parent ?? throw new UnreachableException();

            // We back up the last SelectedReplacementTag's IndexPath.
            var savedSelectedReplacementTag = SingleSelectedReplacementTag.GetIndexPath(ReplacementTags);

            // ...and the actual deleting is dealt with by NBTModel, convenient!
            if (!SingleSelectedReplacementTag.DataNode.DeleteNode()) throw new UnreachableException();

            // We do have to deal with refreshing the parent ourselves, though...
            await parent.RefreshChildNodesAsync();

            // And clear the SelectedReplacementTags, as they're invalid now.
            SelectedReplacementTags.Clear();

            // And finally, we restore our SelectedReplacementTags using our IndexPath.
            var restoredSelectedTreeNode =
                NodeStateRestorer.GetByIndexPath(ReplacementTags, savedSelectedReplacementTag);
            SelectedReplacementTags.Add(restoredSelectedTreeNode ?? parent);
        });
    }

    // This does the per-Node Replacing, and is often called from outside the Dialog. 
    internal async Task Replace(TreeNode node, IReadOnlyList<TagDataNode?> matchedTags)
    {
        // Check if SubNodes is null, and throw if so.
        var subNodes = ReplacementTags[0].SubNodes ?? throw new UnreachableException();

        // Check if the Node to execute the Replace on is a Compound, and throw if not.
        if (node.DataNode is not TagCompoundDataNode container) throw new UnreachableException();

        // We create a list of our ReplacementTags' DataNodes, to simplify working with them.
        var replacementNodes = subNodes.Where(treeNode => !treeNode.IsPlaceholder).Select(treeNode => treeNode.DataNode)
            .OfType<TagDataNode>().ToList();

        // Then we create a HashSet for collisions...
        var deleted = new HashSet<TagDataNode>();

        // ...and for each collision (AKA TreeNode with the exact same name as one of our ReplacementTags)...
        foreach (var collision in replacementNodes.Select(replace =>
                         container.Nodes.OfType<TagDataNode>()
                             .FirstOrDefault(dataNode => dataNode.NodeName == replace.NodeName))
                     .OfType<TagDataNode>())
        {
            // ...we add it to our deleted HashSet...
            deleted.Add(collision);

            // ...then let NBTModel do the actual Deleting.
            if (!collision.DeleteNode()) throw new UnreachableException();
        }

        // If the user chose to Delete all the matches...
        if (DeleteMatchedTags)
            // ...then we get NBTModel to Delete them, unless they were already Deleted as a collision.
            if (matchedTags.OfType<TagDataNode>().Any(matched => !deleted.Contains(matched) && !matched.DeleteNode()))
                throw new UnreachableException();

        // After we finished cleaning up, we Add our ReplacementTags.
        foreach (var replace in replacementNodes) container.AddTag(replace.Tag.Copy(), replace.NodeName);

        // And refresh its parent.
        await node.RefreshChildNodesAsync();
    }

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        // Check if SubNodes is null, and throw if so.
        if (MainViewModel.SingleSelectedTreeNode?.SubNodes is null || RootRule.Rule is not GroupRule groupRule)
            throw new UnreachableException();

        // We block the UI to prevent the user from doing anything while we process the search.
        InProgress = true;

        // And we create our NodeSearcherAdvanced.
        var find = new NodeSearcherAdvanced(MainViewModel.SingleSelectedTreeNode, groupRule);

        // Then we try to Find our first instance of the searched parameters.
        var found = await find.FindNextAsync();

        // If we Find one... 
        if (found is not null)
        {
            // Then we can suppose there are even more things to Find, and thus we save the state in the MainViewModel.
            MainViewModel.NodeSearcher = find;

            // We require this so we can call the ApplyReplacement method right from the Search Bar.
            MainViewModel.NodeSearcherAdvanced = this;

            // We also set FoundMatch to true, preventing the "No matching tags were found." dialog from showing.
            FoundMatch = true;

            // We start Expanding its tree in reverse.
            await found.ExpandTreeReverseAsync();

            // This is so, when we add it to SelectedTreeNodes, the UI automatically jumps to it.
            MainViewModel.SelectedTreeNodes.Clear();
            MainViewModel.SelectedTreeNodes.Add(found);

            return;
        }

        // If we don't, though, we make sure to clean up any leftover state in the MainViewModel.
        MainViewModel.NodeSearcher = null;
    }

    // Here we define our Nested Dialog Kinds.
    private enum NestedDialogKinds
    {
        None = 0,
        EditTagRule = 1,
        AddReplacementTag = 2,
        EditReplacementTag = 3
    }
}
