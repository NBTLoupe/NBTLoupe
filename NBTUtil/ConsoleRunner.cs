using System;
using System.Collections.Generic;
using System.CommandLine;
using NBTModel;
using NBTUtil.Ops;

namespace NBTUtil;

internal static class ConsoleRunner
{
    private static readonly Dictionary<ConsoleCommand, ConsoleOperation> CommandTable = new()
    {
        { ConsoleCommand.SetValue, new EditOperation() },
        { ConsoleCommand.SetList, new SetListOperation() },
        { ConsoleCommand.Print, new PrintOperation() },
        { ConsoleCommand.PrintTree, new PrintTreeOperation() },
        { ConsoleCommand.Json, new JsonOperation() }
    };

    private static readonly Option<string> PathOption = new("--path")
    {
        Description = "Path to NBT tag from current directory",
        Required = true
    };

    private static readonly Option<bool> TypesOption = new("--types")
    {
        Description = "Show data types when printing tags"
    };

    private static Command PrintCommand
    {
        get
        {
            var command = new Command("print", "Print the value(s) of a tag") { TypesOption };

            command.SetAction(parsed =>
            {
                var options = new ConsoleOptions
                {
                    Command = ConsoleCommand.Print,
                    Path = parsed.GetRequiredValue(PathOption),
                    ShowTypes = parsed.GetValue(TypesOption)
                };
                Execute(options);
            });

            return command;
        }
    }

    private static Command PrintTreeCommand
    {
        get
        {
            var command = new Command("printtree", "Print the NBT tree rooted at a tag") { TypesOption };

            command.SetAction(parsed =>
            {
                var options = new ConsoleOptions
                {
                    Command = ConsoleCommand.PrintTree,
                    Path = parsed.GetRequiredValue(PathOption),
                    ShowTypes = parsed.GetValue(TypesOption)
                };
                Execute(options);
            });

            return command;
        }
    }

    private static Command SetValueCommand
    {
        get
        {
            var valueArg = new Argument<string[]>("values")
                { Description = "One or more values to set", Arity = ArgumentArity.OneOrMore };
            var command = new Command("setvalue", "Set a single tag value") { valueArg };

            command.SetAction(parsed =>
            {
                var options = new ConsoleOptions
                {
                    Command = ConsoleCommand.SetValue,
                    Path = parsed.GetRequiredValue(PathOption)
                };

                var value = parsed.GetValue(valueArg);
                if (value is null) return;
                options.Values.AddRange(value);
                Execute(options);
            });

            return command;
        }
    }

    private static Command SetListCommand
    {
        get
        {
            var valuesArg = new Argument<string[]>("values")
            {
                Description = "One or more values to replace the list contents with", Arity = ArgumentArity.OneOrMore
            };
            var command = new Command("setlist", "Replace a list tag's contents with one or more values") { valuesArg };

            command.SetAction(parsed =>
            {
                var options = new ConsoleOptions
                {
                    Command = ConsoleCommand.SetList,
                    Path = parsed.GetRequiredValue(PathOption)
                };

                var value = parsed.GetValue(valuesArg);
                if (value is null) return;
                options.Values.AddRange(value);
                Execute(options);
            });

            return command;
        }
    }

    private static Command JsonCommand
    {
        get
        {
            var outputArg = new Argument<string>("output")
            {
                Description = "Path to the JSON output file"
            };
            var command = new Command("json", "Export the NBT tree rooted at a tag as JSON") { outputArg };

            command.SetAction(parsed =>
            {
                var options = new ConsoleOptions
                {
                    Command = ConsoleCommand.Json,
                    Path = parsed.GetRequiredValue(PathOption)
                };

                var value = parsed.GetValue(outputArg);
                if (value is null) return;
                options.Values.Add(value);
                Execute(options);
            });

            return command;
        }
    }

    public static int Run(string[] args)
    {
        var rootCommand = new RootCommand("neoNBTUtil - A modernised version of the unfinished NBTUtil")
        {
            PathOption,
            PrintCommand,
            PrintTreeCommand,
            SetValueCommand,
            SetListCommand,
            JsonCommand
        };

        return rootCommand.Parse(args).Invoke();
    }

    private static void Execute(ConsoleOptions options)
    {
        var op = CommandTable[options.Command];
        if (!op.OptionsValid(options))
        {
            Console.Error.WriteLine("Error: Invalid options specified for the given command");
            return;
        }

        var successCount = 0;
        var failCount = 0;

        foreach (var targetNode in new NbtPathEnumerator(options.Path))
        {
            if (!op.CanProcess(targetNode))
            {
                Console.WriteLine(targetNode.NodePath + ": ERROR (invalid command)");
                failCount++;
            }

            if (!op.Process(targetNode, options))
            {
                Console.WriteLine(targetNode.NodePath + ": ERROR (apply)");
                failCount++;
            }

            targetNode.Root.Save();

            Console.WriteLine(targetNode.NodePath + ": OK");
            successCount++;
        }

        Console.WriteLine("Operation complete.  Nodes succeeded: {0}  Nodes failed: {1}", successCount, failCount);
    }
}
