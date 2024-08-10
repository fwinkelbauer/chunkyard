namespace Chunkyard.Cli;

/// <summary>
/// Parses arguments into a command and calls an appropriate handler.
/// </summary>
public class CommandHandler
{
    private readonly List<ICommandParser> _parsers;
    private readonly Dictionary<Type, Func<object, int>> _handlers;

    public CommandHandler()
    {
        _parsers = new();
        _handlers = new();
    }

    public int Handle(params string[] args)
    {
        var parser = new CommandParser(_parsers);
        var command = parser.Parse(args);

        return _handlers[command.GetType()](command);
    }

    public CommandHandler With<T>(ICommandParser parser, Action<T> handler)
    {
        _parsers.Add(parser);

        return Use<T>(t =>
        {
            handler(t);
            return 0;
        });
    }

    public CommandHandler Use<T>(Func<T, int> handler)
    {
        _handlers[typeof(T)] = o => handler((T)o);

        return this;
    }
}
