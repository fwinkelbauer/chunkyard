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
        _help = new(GetInfo(typeof(CommandParser).Assembly));

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
        else if (string.IsNullOrEmpty(arg.Command))
        {
            _help.AddError("No command provided");

            return _help.Build();
        }
        else if (_parsers.TryGetValue(arg.Command, out var parser))
        {
            var consumer = new FlagConsumer(arg.Flags, _help);
            var command = parser.Parse(consumer);

            return consumer.HelpNeeded(out var help) || command == null
                ? help
                : command;
        }
        else
        {
            _help.AddError($"Unknown command: {arg.Command}");

            return _help.Build();
        }
    }

    private static string GetInfo(Assembly assembly)
    {
        var textInfo = new CultureInfo("en-US").TextInfo;

        var product = assembly
            .GetCustomAttributes<AssemblyProductAttribute>()
            .First()
            .Product;

        var version = assembly
            .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            .First()
            .InformationalVersion;

        return $"{textInfo.ToTitleCase(product)} v{version}";
    }
}
