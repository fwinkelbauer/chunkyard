namespace Chunkyard.Cli;

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

        var parser = _parsers.FirstOrDefault(
            p => p.Command.Equals(arg.Command));

        return parser == null
            ? new HelpCommand(
                _helpTexts,
                new[] { $"Unknown command: {arg.Command}" })
            : parser.Parse(new ArgConsumer(arg));
    }
}
