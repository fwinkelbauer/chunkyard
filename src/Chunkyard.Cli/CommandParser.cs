namespace Chunkyard.Cli;

/// <summary>
/// This class dispatches args to instances of <see cref="ICommandParser"/>.
/// Returns a <see cref="HelpCommand"/> if no matching command parser can be
/// found.
/// </summary>
public sealed class CommandParser
{
    private readonly IReadOnlyCollection<ICommandParser> _parsers;
    private readonly IReadOnlyCollection<HelpText> _helpTexts;

    public CommandParser(params ICommandParser[] parsers)
    {
        _parsers = parsers;

        _helpTexts = _parsers
            .Select(p => new HelpText(p.Command, p.Info))
            .ToArray();
    }

    public object Parse(params string[] args)
    {
        var arg = Arg.Parse(args);

        if (arg == null)
        {
            return new HelpCommand(_helpTexts, Array.Empty<string>());
        }

        var consumer = new FlagConsumer(arg.Flags);

        if (arg.Command.Equals("help"))
        {
            return new HelpCommand(_helpTexts, Array.Empty<string>());
        }

        var parser = _parsers.FirstOrDefault(
            p => p.Command.Equals(arg.Command));

        if (parser == null)
        {
            return new HelpCommand(
                _helpTexts,
                new[] { $"Unknown command: {arg.Command}" });
        }

        var command = parser.Parse(consumer);

        var helpRequested = consumer.TryBool("--help", "Print usage information", out var help)
            && help;

        if (helpRequested
            || !consumer.TryEmpty()
            || consumer.Help.Errors.Any())
        {
            return consumer.Help;
        }

        return command;
    }
}
