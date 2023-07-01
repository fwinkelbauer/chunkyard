namespace Chunkyard.Make;

public static class CommandParser
{
    public static IReadOnlyCollection<Usage> Usages
        => Parsers.Select(p => new Usage(p.Command, p.Info)).ToArray();

    public static IReadOnlyCollection<ICommandParser> Parsers => new ICommandParser[]
    {
        new BuildCommandParser(),
        new CheckCommandParser(),
        new CleanCommandParser(),
        new FormatCommandParser(),
        new HelpCommandParser(),
        new PublishCommandParser(),
        new ReleaseCommandParser()
    };

    public static ICommand Parse(params string[] args)
    {
        var argResult = Arg.Parse(args);

        if (argResult.Value == null)
        {
            return new HelpCommand(Usages, argResult.Errors);
        }

        var parser = Parsers.FirstOrDefault(
            p => p.Command.Equals(argResult.Value.Command));

        return parser == null
            ? new HelpCommand(
                Usages,
                new[] { $"Unknown command: {argResult.Value.Command}" })
            : parser.Parse(argResult.Value);
    }
}

public interface ICommandParser
{
    string Command { get; }

    string Info { get; }

    ICommand Parse(Arg arg);
}

public sealed class BuildCommandParser : ICommandParser
{
    public string Command => "build";

    public string Info => "Build the repository";

    public ICommand Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        return consumer.IsConsumed()
            ? new BuildCommand()
            : new HelpCommand(consumer.Usages, consumer.Errors);
    }
}

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check for dependency updates";

    public ICommand Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        return consumer.IsConsumed()
            ? new CheckCommand()
            : new HelpCommand(consumer.Usages, consumer.Errors);
    }
}

public sealed class CleanCommandParser : ICommandParser
{
    public string Command => "clean";

    public string Info => "Clean the repository";

    public ICommand Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        return consumer.IsConsumed()
            ? new CleanCommand()
            : new HelpCommand(consumer.Usages, consumer.Errors);
    }
}

public sealed class FormatCommandParser : ICommandParser
{
    public string Command => "format";

    public string Info => "Run the formatter";

    public ICommand Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        return consumer.IsConsumed()
            ? new FormatCommand()
            : new HelpCommand(consumer.Usages, consumer.Errors);
    }
}

public sealed class HelpCommandParser : ICommandParser
{
    public string Command => "help";

    public string Info => "Print usage information";

    public ICommand Parse(Arg arg)
        => new HelpCommand(CommandParser.Usages, Array.Empty<string>());
}

public sealed class PublishCommandParser : ICommandParser
{
    public string Command => "publish";

    public string Info => "Publish the main project";

    public ICommand Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        return consumer.IsConsumed()
            ? new PublishCommand()
            : new HelpCommand(consumer.Usages, consumer.Errors);
    }
}

public sealed class ReleaseCommandParser : ICommandParser
{
    public string Command => "release";

    public string Info => "Create a release commit";

    public ICommand Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        return consumer.IsConsumed()
            ? new ReleaseCommand()
            : new HelpCommand(consumer.Usages, consumer.Errors);
    }
}
