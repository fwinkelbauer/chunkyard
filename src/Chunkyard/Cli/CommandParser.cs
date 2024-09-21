namespace Chunkyard.Cli;

/// <summary>
/// This class dispatches args to instances of <see cref="ICommandParser"/>.
/// Returns a <see cref="HelpCommand"/> if no matching command parser can be
/// found.
/// </summary>
public sealed class CommandParser
{
    private readonly Dictionary<string, ICommandParser> _parsers;
    private readonly HelpCommandBuilder _help;

    public CommandParser(params ICommandParser[] parsers)
    {
        _parsers = parsers.ToDictionary(p => p.Command, p => p);
        _help = new($"Chunkyard {GetVersion()}");

        foreach (var parser in parsers)
        {
            _help.AddCommand(parser.Command, parser.Info);
        }
    }

    public ICommand Parse(params string[] args)
    {
        var arg = Args.Parse(args);

        if (arg == null)
        {
            return _help.Build();
        }
        else if (_parsers.TryGetValue(arg.Command, out var parser))
        {
            return parser.Parse(
                new FlagConsumer(arg.Flags, _help));
        }
        else
        {
            _help.AddError($"Unknown command: {arg.Command}");

            return _help.Build();
        }
    }

    private static string GetVersion()
    {
        var attribute = typeof(Program).Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
            .First();

        return ((AssemblyInformationalVersionAttribute)attribute)
            .InformationalVersion;
    }
}
