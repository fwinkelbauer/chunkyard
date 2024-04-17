namespace Chunkyard.Cli;

/// <summary>
/// This class dispatches args to instances of <see cref="ICommandParser"/>.
/// Returns a <see cref="HelpCommand"/> if no matching command parser can be
/// found.
/// </summary>
public sealed class CommandParser
{
    private readonly List<ICommandParser> _parsers;
    private readonly List<HelpText> _helpTexts;

    public CommandParser(IReadOnlyCollection<ICommandParser> parsers)
    {
        _parsers = new List<ICommandParser>(parsers);

        _helpTexts = _parsers
            .Select(p => new HelpText(p.Command, p.Info))
            .ToList();

        var helpCommand = "help";
        var helpInfo = "Print all available commands";

        _helpTexts.Add(new HelpText(helpCommand, helpInfo));

        _parsers.Add(
            new SimpleCommandParser(
                helpCommand,
                helpInfo,
                new HelpCommand(_helpTexts, Array.Empty<string>())));

        _parsers = _parsers.OrderBy(p => p.Command).ToList();
        _helpTexts = _helpTexts.OrderBy(h => h.Topic).ToList();
    }

    public object Parse(params string[] args)
    {
        var arg = Args.Parse(args);

        if (arg == null)
        {
            return new HelpCommand(_helpTexts, Array.Empty<string>());
        }

        var consumer = new FlagConsumer(arg.Flags);

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
            | !consumer.TryEmpty()
            | consumer.Help.Errors.Any())
        {
            return consumer.Help;
        }

        return command;
    }
}
