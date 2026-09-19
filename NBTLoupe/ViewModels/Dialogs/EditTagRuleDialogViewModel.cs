using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core.TreeNodes;
using NBTLoupe.ViewModels.Main;
using NBTModel.Search;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the EditTagRule Dialog!
internal partial class EditTagRuleDialogViewModel : DialogHostViewModel
{
    // This is so we execute the right code path depending on whether we're Adding a new Rule or Editing one!
    private readonly bool _isAdd;

    // Here we set up the Dialog!
    internal EditTagRuleDialogViewModel(MainViewModel mainViewModel, FindAdvancedDialogViewModel parent) : base(
        mainViewModel, parent)
    {
        // If the SelectedRuleNode is the future Parent, then we're Adding and not Editing! We need to know this later on.
        _isAdd = SelectedRuleNode.Rule is GroupRule;

        // Set the context-accurate Title
        TitleText = $"{(_isAdd ? "Add" : "Edit")} {(RelevantTagType is { } tagType ?
                tagType.GetFriendlyTag() : "Wildcard")
        } Rule";

        // And we prepopulate the Dialog with the Rule's current Name, appropriate Operators, and Value (if we're Editing)!
        (TagName, SelectedOperator, TagValue) = SelectedRuleNode.Rule switch
        {
            WildcardRule rule => (rule.Name, SearchRule.WildcardOpStrings[rule.Operator], rule.Value),
            ByteTagRule rule => (rule.Name, SearchRule.NumericOpStrings[rule.Operator], rule.Value.ToString()),
            ShortTagRule rule => (rule.Name, SearchRule.NumericOpStrings[rule.Operator], rule.Value.ToString()),
            IntTagRule rule => (rule.Name, SearchRule.NumericOpStrings[rule.Operator], rule.Value.ToString()),
            LongTagRule rule => (rule.Name, SearchRule.NumericOpStrings[rule.Operator], rule.Value.ToString()),
            FloatTagRule rule => (rule.Name, SearchRule.NumericOpStrings[rule.Operator],
                rule.Value.ToString(CultureInfo.InvariantCulture)),
            DoubleTagRule rule => (rule.Name, SearchRule.NumericOpStrings[rule.Operator],
                rule.Value.ToString(CultureInfo.InvariantCulture)),
            StringTagRule rule => (rule.Name, SearchRule.StringOpStrings[rule.Operator], rule.Value),
            _ => (TagName, SelectedOperator, TagValue)
        };

        // If the Operator was set to "ANY", we clean the Value to not confuse the user.
        TagValue = SelectedOperator != "ANY" ? TagValue : "";
    }

    // Here's all the fields we bind to in the XAML...
    // The Title TextBlock...
    internal string TitleText { get; }

    // The Tag Name TextBox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    internal partial string? TagName { get; set; }

    // The Tag Name TextBox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    internal partial string? TagValue { get; set; }

    // The Operators CheckBox's values...
    internal ObservableCollection<string> Operators =>
    [
        .. RelevantTagType switch
        {
            null => SearchRule.WildcardOpStrings.Values as IEnumerable<string>,
            TagType.TAG_STRING => SearchRule.StringOpStrings.Values,
            _ => SearchRule.NumericOpStrings.Values
        }
    ];

    // ...and its selection.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    internal partial string? SelectedOperator { get; set; }

    // We make sure we weren't Nested by the wrong Dialog; which shouldn't ever happen, and we throw if so. 
    private FindAdvancedDialogViewModel ParentDialog =>
        Parent as FindAdvancedDialogViewModel ?? throw new UnreachableException();

    // And after doing so, we grab the SelectedRuleNode, which should never be null so we throw if it is.
    private TreeRule SelectedRuleNode => ParentDialog.SelectedRuleNode ?? throw new UnreachableException();

    // When Adding a Rule, we get our special RuleKind directly from the Binding! But we don't when Editing, so we convert it from the Rule Type. This allows us to reuse BuildRule.
    private TagType? RelevantTagType => _isAdd
        ? ParentDialog.NewRuleTagType
        : (SelectedRuleNode.Rule as TagRule)?.TagType;

    // And here's where our Validation magic happens!
    internal override bool IsOkEnabled => !string.IsNullOrEmpty(TagName) && !string.IsNullOrEmpty(SelectedOperator) &&
                                          (SelectedOperator == "ANY" || (!string.IsNullOrEmpty(TagValue) &&
                                                                         ValidateTagValue(RelevantTagType, TagValue)));

    // And here's where we validate a new TagValue!
    private static bool ValidateTagValue(TagType? tagType, string? value)
    {
        return tagType switch
        {
            null or TagType.TAG_STRING => true,

            TagType.TAG_BYTE => sbyte.TryParse(value, out _),
            TagType.TAG_SHORT => short.TryParse(value, out _),
            TagType.TAG_INT => int.TryParse(value, out _),
            TagType.TAG_LONG => long.TryParse(value, out _),
            TagType.TAG_FLOAT => float.TryParse(value, out _),
            TagType.TAG_DOUBLE => double.TryParse(value, out _),
            _ => false
        };
    }

    // And this is how we build our new Rules! It's pretty self-explanatory.
    private static SearchRule BuildRule(TagType? tagType, string name, string op, string value)
    {
        return tagType switch
        {
            null => new WildcardRule
            {
                Name = name,
                Operator = SearchRule.WildcardOpFromString[op],
                Value = value
            },
            TagType.TAG_BYTE => new ByteTagRule
            {
                Name = name,
                Operator = SearchRule.NumericOpFromString[op],
                Value = sbyte.Parse(value)
            },
            TagType.TAG_SHORT => new ShortTagRule
            {
                Name = name,
                Operator = SearchRule.NumericOpFromString[op],
                Value = short.Parse(value)
            },
            TagType.TAG_INT => new IntTagRule
            {
                Name = name,
                Operator = SearchRule.NumericOpFromString[op],
                Value = int.Parse(value)
            },
            TagType.TAG_LONG => new LongTagRule
            {
                Name = name,
                Operator = SearchRule.NumericOpFromString[op],
                Value = long.Parse(value)
            },
            TagType.TAG_FLOAT => new FloatTagRule
            {
                Name = name,
                Operator = SearchRule.NumericOpFromString[op],
                Value = float.Parse(value)
            },
            TagType.TAG_DOUBLE => new DoubleTagRule
            {
                Name = name,
                Operator = SearchRule.NumericOpFromString[op],
                Value = double.Parse(value)
            },
            TagType.TAG_STRING => new StringTagRule
            {
                Name = name,
                Operator = SearchRule.StringOpFromString[op],
                Value = value
            },
            _ => throw new UnreachableException()
        };
    }

    // And here's the actual magic! The OK button!
    internal override Task ExecuteAsync()
    {
        // BuildRule requires a valid TagValue, even if it is ignored with the "ANY" operator. Due to this, we just set it to "0" (which is universal) in these cases, so the Parsing succeeds.
        if (SelectedOperator == "ANY") TagValue = "0";

        // We should always have all the values populated, so we throw if any of them isn't.
        if (string.IsNullOrEmpty(TagName) || string.IsNullOrEmpty(SelectedOperator) || string.IsNullOrEmpty(TagValue))
            throw new UnreachableException();

        // Then we can finally build the Rule using the given data!
        var newRule = BuildRule(RelevantTagType, TagName, SelectedOperator, TagValue);

        // If we're Adding a new Rule...
        if (_isAdd)
        {
            // ...we add the just-built Rule to the Parent, then automatically select it.
            ParentDialog.SelectedRuleNode = SelectedRuleNode.Add(newRule);
        }
        // But if we're Editing an existing Rule...
        else
        {
            // ...we replace its internal Rule with our new Rule... 
            ParentDialog.SelectedRuleNode?.Rule = newRule;

            // ...and force a UI refresh, as the Rule isn't UI-wise.
            ParentDialog.SelectedRuleNode?.RefreshTitle();
        }

        return Task.CompletedTask;
    }
}
