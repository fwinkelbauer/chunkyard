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
        try
        {
            var parser = new CommandParser(_parsers);
            var command = parser.Parse(args);

            return _handlers[command.GetType()](command);
        }
        catch (Exception e)
        {
            return _handlers[typeof(Exception)](e);
        }
    }

    public CommandHandler With<T>(ICommandParser parser, Func<T, int> handler)
    {
        _parsers.Add(parser);

        Use<T>(handler);

        return this;
    }

    public CommandHandler With<T>(ICommandParser parser, Func<int> handler)
    {
        _parsers.Add(parser);

        Use<T>(handler);

        return this;
    }

    public CommandHandler With<T>(ICommandParser parser, Action<T> handler)
    {
        _parsers.Add(parser);

        Use<T>(handler);

        return this;
    }

    public CommandHandler With<T>(ICommandParser parser, Action handler)
    {
        _parsers.Add(parser);

        Use<T>(handler);

        return this;
    }

    public CommandHandler Use<T>(Func<T, int> handler)
    {
        _handlers[typeof(T)] = o => handler((T)o);

        return this;
    }

    public CommandHandler Use<T>(Func<int> handler)
    {
        _handlers[typeof(T)] = _ => handler();

        return this;
    }

    public CommandHandler Use<T>(Action<T> handler)
    {
        return Use<T>(t =>
        {
            handler(t);

            return 0;
        });
    }

    public CommandHandler Use<T>(Action handler)
    {
        return Use<T>(_ => handler());
    }
}
