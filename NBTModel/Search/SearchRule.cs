using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NBTModel.Data.Nodes;
using Substrate.Nbt;

namespace NBTModel.Search;

public enum NumericOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    Any
}

public enum StringOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    Any
}

public enum WildcardOperator
{
    Equals,
    NotEquals,
    Any
}

public abstract class SearchRule
{
    protected const double Epsilon = 1e-5;

    public static readonly Dictionary<NumericOperator, string> NumericOpStrings = new()
    {
        { NumericOperator.Equals, "=" },
        { NumericOperator.NotEquals, "!=" },
        { NumericOperator.GreaterThan, ">" },
        { NumericOperator.LessThan, "<" },
        { NumericOperator.Any, "ANY" }
    };

    public static readonly Dictionary<StringOperator, string> StringOpStrings = new()
    {
        { StringOperator.Equals, "=" },
        { StringOperator.NotEquals, "!=" },
        { StringOperator.Contains, "Contains" },
        { StringOperator.NotContains, "Does not Contain" },
        { StringOperator.StartsWith, "Begins With" },
        { StringOperator.EndsWith, "Ends With" },
        { StringOperator.Any, "ANY" }
    };

    public static readonly Dictionary<WildcardOperator, string> WildcardOpStrings = new()
    {
        { WildcardOperator.Equals, "=" },
        { WildcardOperator.NotEquals, "!=" },
        { WildcardOperator.Any, "ANY" }
    };

    public static readonly Dictionary<string, NumericOperator> NumericOpFromString =
        NumericOpStrings.ToDictionary(x => x.Value, x => x.Key);

    public static readonly Dictionary<string, StringOperator> StringOpFromString =
        StringOpStrings.ToDictionary(x => x.Value, x => x.Key);

    public static readonly Dictionary<string, WildcardOperator> WildcardOpFromString =
        WildcardOpStrings.ToDictionary(x => x.Value, x => x.Key);

    public abstract string NodeDisplay { get; }

    public virtual bool CanAddRules => false;

    public virtual bool Matches(TagCompoundDataNode container, List<TagDataNode?> matchedNodes)
    {
        return false;
    }

    protected static TagDataNode? GetChild(TagCompoundDataNode container, string name)
    {
        foreach (var child in container.Nodes)
            if (child is TagDataNode tagChild && tagChild.NodeName == name)
                return tagChild;

        return null;
    }
}

public abstract class GroupRule : SearchRule
{
    public List<SearchRule> Rules { get; } = [];

    public override bool CanAddRules => true;
}

public class UnionRule : GroupRule
{
    public override string NodeDisplay => "Match Any";

    public override bool Matches(TagCompoundDataNode container, List<TagDataNode?> matchedNodes)
    {
        return Rules.Any(rule => rule.Matches(container, matchedNodes));
    }
}

public class IntersectRule : GroupRule
{
    public override string NodeDisplay => "Match All";

    public override bool Matches(TagCompoundDataNode container, List<TagDataNode?> matchedNodes)
    {
        return Rules.All(rule => rule.Matches(container, matchedNodes));
    }
}

public class RootRule : IntersectRule
{
    public override string NodeDisplay => "Search Rules";
}

public abstract class TagRule : SearchRule
{
    public abstract TagType TagType { get; }
    public required string Name { get; init; }

    protected static T? LookupTag<T>(TagCompoundDataNode container, string name)
        where T : TagNode
    {
        return container.NamedTagContainer.GetTagNode(name) as T;
    }
}

public abstract class IntegralTagRule<T> : TagRule
    where T : TagNode
{
    public long Value { get; init; }

    public NumericOperator Operator { get; init; }

    public override string NodeDisplay =>
        $"{Name} {NumericOpStrings[Operator]} {(Operator != NumericOperator.Any ? Value.ToString() : "")}";

    public override bool Matches(TagCompoundDataNode container, List<TagDataNode?> matchedNodes)
    {
        var childNode = GetChild(container, Name);
        var data = LookupTag<T>(container, Name);
        if (data == null)
            return false;

        switch (Operator)
        {
            case NumericOperator.Equals:
                if (data.ToTagLong().Data != Value)
                    return false;
                break;
            case NumericOperator.NotEquals:
                if (data.ToTagLong().Data == Value)
                    return false;
                break;
            case NumericOperator.GreaterThan:
                if (data.ToTagLong().Data <= Value)
                    return false;
                break;
            case NumericOperator.LessThan:
                if (data.ToTagLong().Data >= Value)
                    return false;
                break;
            case NumericOperator.Any:
                break;
            default:
                return false;
        }

        if (!matchedNodes.Contains(childNode))
            matchedNodes.Add(childNode);

        return true;
    }
}

public class ByteTagRule : IntegralTagRule<TagNodeByte>
{
    public override TagType TagType => TagType.TAG_BYTE;
}

public class ShortTagRule : IntegralTagRule<TagNodeShort>
{
    public override TagType TagType => TagType.TAG_SHORT;
}

public class IntTagRule : IntegralTagRule<TagNodeInt>
{
    public override TagType TagType => TagType.TAG_INT;
}

public class LongTagRule : IntegralTagRule<TagNodeLong>
{
    public override TagType TagType => TagType.TAG_LONG;
}

public abstract class FloatTagRule<T> : TagRule
    where T : TagNode
{
    public double Value { get; init; }

    public NumericOperator Operator { get; init; }

    public override string NodeDisplay =>
        $"{Name} {NumericOpStrings[Operator]} {(Operator != NumericOperator.Any ? Value.ToString(CultureInfo.InvariantCulture) : "")}";

    public override bool Matches(TagCompoundDataNode container, List<TagDataNode?> matchedNodes)
    {
        var childNode = GetChild(container, Name);
        var data = LookupTag<T>(container, Name);
        if (data == null)
            return false;

        switch (Operator)
        {
            case NumericOperator.Equals:
                if (Math.Abs(data.ToTagDouble().Data - Value) > Epsilon)
                    return false;
                break;
            case NumericOperator.NotEquals:
                if (Math.Abs(data.ToTagDouble().Data - Value) <= Epsilon)
                    return false;
                break;
            case NumericOperator.GreaterThan:
                if (data.ToTagDouble().Data <= Value)
                    return false;
                break;
            case NumericOperator.LessThan:
                if (data.ToTagDouble().Data >= Value)
                    return false;
                break;
            case NumericOperator.Any:
                break;
            default:
                return false;
        }

        if (!matchedNodes.Contains(childNode))
            matchedNodes.Add(childNode);

        return true;
    }
}

public class FloatTagRule : FloatTagRule<TagNodeFloat>
{
    public override TagType TagType => TagType.TAG_FLOAT;
}

public class DoubleTagRule : FloatTagRule<TagNodeDouble>
{
    public override TagType TagType => TagType.TAG_DOUBLE;
}

public class StringTagRule : TagRule
{
    public override TagType TagType => TagType.TAG_STRING;

    public required string Value { get; init; }

    public StringOperator Operator { get; init; }

    public override string NodeDisplay =>
        $"{Name} {StringOpStrings[Operator]} {(Operator != StringOperator.Any ? '"' + Value + '"' : "")}";

    public override bool Matches(TagCompoundDataNode container, List<TagDataNode?> matchedNodes)
    {
        var childNode = GetChild(container, Name);
        var data = LookupTag<TagNodeString>(container, Name);
        if (data == null)
            return false;

        switch (Operator)
        {
            case StringOperator.Equals:
                if (data.ToTagString().Data != Value)
                    return false;
                break;
            case StringOperator.NotEquals:
                if (data.ToTagString().Data == Value)
                    return false;
                break;
            case StringOperator.Contains:
                if (!data.ToTagString().Data.Contains(Value))
                    return false;
                break;
            case StringOperator.NotContains:
                if (data.ToTagString().Data.Contains(Value))
                    return false;
                break;
            case StringOperator.StartsWith:
                if (!data.ToTagString().Data.StartsWith(Value))
                    return false;
                break;
            case StringOperator.EndsWith:
                if (!data.ToTagString().Data.EndsWith(Value))
                    return false;
                break;
            case StringOperator.Any:
                break;
            default:
                return false;
        }

        if (!matchedNodes.Contains(childNode))
            matchedNodes.Add(childNode);

        return true;
    }
}

public class WildcardRule : SearchRule
{
    public required string Name { get; init; }
    public required string Value { get; init; }

    public WildcardOperator Operator { get; init; }

    public override string NodeDisplay =>
        $"{Name} {WildcardOpStrings[Operator]} {(Operator != WildcardOperator.Any ? Value : "")}";

    public override bool Matches(TagCompoundDataNode container, List<TagDataNode?> matchedNodes)
    {
        var childNode = GetChild(container, Name);
        var tag = container.NamedTagContainer.GetTagNode(Name);
        if (tag == null)
            return false;

        try
        {
            switch (tag.GetTagType())
            {
                case TagType.TAG_BYTE:
                case TagType.TAG_INT:
                case TagType.TAG_LONG:
                case TagType.TAG_SHORT:
                    switch (Operator)
                    {
                        case WildcardOperator.Equals:
                            if (long.Parse(Value) != tag.ToTagLong())
                                return false;
                            break;
                        case WildcardOperator.NotEquals:
                            if (long.Parse(Value) == tag.ToTagLong())
                                return false;
                            break;
                    }

                    if (!matchedNodes.Contains(childNode))
                        matchedNodes.Add(childNode);
                    return true;
                case TagType.TAG_FLOAT:
                case TagType.TAG_DOUBLE:
                    switch (Operator)
                    {
                        case WildcardOperator.Equals:
                            if (Math.Abs(double.Parse(Value) - tag.ToTagDouble().Data) > Epsilon)
                                return false;
                            break;
                        case WildcardOperator.NotEquals:
                            if (Math.Abs(double.Parse(Value) - tag.ToTagDouble().Data) <= Epsilon)
                                return false;
                            break;
                    }

                    if (!matchedNodes.Contains(childNode))
                        matchedNodes.Add(childNode);
                    return true;
                case TagType.TAG_STRING:
                    switch (Operator)
                    {
                        case WildcardOperator.Equals:
                            if (Value != tag.ToTagString().Data)
                                return false;
                            break;
                        case WildcardOperator.NotEquals:
                            if (Value == tag.ToTagString().Data)
                                return false;
                            break;
                    }

                    if (!matchedNodes.Contains(childNode))
                        matchedNodes.Add(childNode);
                    return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }
}
