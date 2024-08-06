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
    }

    public object Parse(params string[] args)
    {
        var arg = Args.Parse(args);

        if (arg == null)
        {
            return new HelpCommand(_infos, Array.Empty<string>());
        }
        else if (_parsers.TryGetValue(arg.Command, out var parser))
        {
            return parser.Parse(
                new FlagConsumer(arg.Flags));
        }
        else
        {
            return new HelpCommand(
                _infos,
                new[] { $"Unknown command: {arg.Command}" });
        }
    }
}
