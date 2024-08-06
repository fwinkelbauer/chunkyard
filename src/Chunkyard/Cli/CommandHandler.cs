namespace Chunkyard.Cli;

/// <summary>
/// Parses arguments into a command and calls an appropriate handler.
/// </summary>
public class CommandHandler
{
    private const int ExitCodeOk = 0;
    private const int ExitCodeError = 1;

    private readonly List<ICommandParser> _parsers;
    private readonly Dictionary<Type, Func<object, int>> _handlers;

    public CommandHandler()
    {
        _parsers = new();
        _handlers = new();

        Use<HelpCommand>(WriteHelp);
        Use<VersionCommand>(WriteVersion);
        Use<Exception>(WriteError);
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

            return ExitCodeOk;
        });
    }

    public CommandHandler Use<T>(Action handler)
    {
        return Use<T>(_ => handler());
    }

    private static int WriteError(Exception e)
    {
        Console.Error.WriteLine("Error:");

        IEnumerable<Exception> exceptions = e is AggregateException a
            ? a.InnerExceptions
            : new[] { e };

        foreach (var exception in exceptions)
        {
            Console.Error.WriteLine(exception.ToString());
        }

        return ExitCodeError;
    }

    private static int WriteHelp(HelpCommand c)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  <command> <flags>");

        if (c.Infos.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Help:");

            foreach (var info in c.Infos.OrderBy(i => i.Key))
            {
                Console.Error.WriteLine($"  {info.Key}");
                Console.Error.WriteLine($"    {info.Value}");
            }
        }

        if (c.Errors.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(c.Errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in c.Errors.OrderBy(e => e))
            {
                Console.Error.WriteLine($"  {error}");
            }
        }

        Console.Error.WriteLine();

        return ExitCodeError;
    }

    private static int WriteVersion()
    {
        var attribute = typeof(VersionCommand).Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
            .First();

        var version = ((AssemblyInformationalVersionAttribute)attribute)
            .InformationalVersion;

        Console.Error.WriteLine(version);

        return ExitCodeOk;
    }
}
