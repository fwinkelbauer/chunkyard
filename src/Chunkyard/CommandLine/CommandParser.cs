namespace Chunkyard.CommandLine;

/// <summary>
/// This class dispatches args to instances of a set of command parsers. Returns
/// a <see cref="HelpCommand"/> if no matching parser can be found.
/// </summary>
public sealed class CommandParser
{
    private readonly Dictionary<string, Func<FlagConsumer, ICommand?>> _parsers;
    private readonly HelpCommandBuilder _help;

    public CommandParser()
    {
        _parsers = new();
        _help = new(GetInfo(typeof(CommandParser).Assembly));
    }

    public CommandParser With(
        string command,
        string info,
        Func<FlagConsumer, ICommand?> parser)
    {
        _parsers[command] = parser;
        _help.AddCommand(command, info);

        return this;
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
            var command = parser(consumer);

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
