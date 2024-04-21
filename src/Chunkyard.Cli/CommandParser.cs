namespace Chunkyard.Cli;

/// <summary>
/// This class dispatches args to instances of <see cref="ICommandParser"/>.
/// Returns a <see cref="HelpCommand"/> if no matching command parser can be
/// found.
/// </summary>
public sealed class CommandParser
{
    private readonly Dictionary<string, ICommandParser> _parsers;
    private readonly Dictionary<string, string> _infos;

    public CommandParser(IReadOnlyCollection<ICommandParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.Command, p => p);
        _infos = parsers.ToDictionary(p => p.Command, p => p.Info);

        var helpCommand = "help";
        var helpInfo = "Print all available commands";

        _infos[helpCommand] = helpInfo;

        _parsers[helpCommand] = new SimpleCommandParser(
            helpCommand,
            helpInfo,
            new HelpCommand(_infos, Array.Empty<string>()));
    }

    public object Parse(params string[] args)
    {
        var arg = Args.Parse(args);

        if (arg == null)
        {
            return new HelpCommand(_infos, Array.Empty<string>());
        }

        if (!_parsers.TryGetValue(arg.Command, out var parser))
        {
            return new HelpCommand(
                _infos,
                new[] { $"Unknown command: {arg.Command}" });
        }

        var consumer = new FlagConsumer(arg.Flags);

        var helpRequested = consumer.TryBool("--help", "Print usage information", out var help)
            && help;

        var command = parser.Parse(consumer);

        if (helpRequested
            | !consumer.TryEmpty()
            | consumer.Help.Errors.Any())
        {
            return consumer.Help;
        }

        return command;
    }
}
