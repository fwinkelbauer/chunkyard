namespace Chunkyard.Cli;

/// <summary>
/// Parses arguments into a command and calls an appropriate handler.
/// </summary>
public class CommandHandler
{
    private readonly List<ICommandParser> _parsers;
    private readonly Dictionary<Type, Action<object>> _handlers;

    public CommandHandler()
    {
        _parsers = new();
        _handlers = new();

        Use<HelpCommand>(Help);
        Use<VersionCommand>(_ => Version());
    }

    public CommandHandler With<T>(ICommandParser parser, Action<T> handler)
    {
        _parsers.Add(parser);

        Use<T>(handler);

        return this;
    }

    public int Handle(params string[] args)
    {
        try
        {
            var parser = new CommandParser(_parsers);
            var command = parser.Parse(args);

            _handlers[command.GetType()](command);

            return command is HelpCommand ? 1 : 0;
        }
        catch (Exception e)
        {
            Error(e);

            return 1;
        }
    }

    private void Use<T>(Action<T> handler)
    {
        _handlers[typeof(T)] = o => handler((T)o);
    }

    private static void Error(Exception e)
    {
        Console.Error.WriteLine("Error:");

        IEnumerable<Exception> exceptions = e is AggregateException a
            ? a.InnerExceptions
            : new[] { e };

        foreach (var exception in exceptions)
        {
            Console.Error.WriteLine(exception.ToString());
        }
    }

    private static void Help(HelpCommand c)
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
    }

    private static void Version()
    {
        var attribute = typeof(VersionCommand).Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
            .First();

        var version = ((AssemblyInformationalVersionAttribute)attribute)
            .InformationalVersion;

        Console.Error.WriteLine(version);
    }
}
