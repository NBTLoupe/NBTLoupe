using System.Collections.Generic;

namespace NBTUtil;

public enum ConsoleCommand
{
    None,
    Print,
    PrintTree,
    SetValue,
    SetList,
    Json
}

internal class ConsoleOptions
{
    public required string Path { get; init; }
    public ConsoleCommand Command { get; init; } = ConsoleCommand.None;
    public List<string> Values { get; } = [];
    public bool ShowTypes { get; init; }
}
