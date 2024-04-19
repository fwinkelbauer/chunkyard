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
    }

    public CommandHandler With<T>(ICommandParser parser, Action<T> handler)
    {
        _parsers.Add(parser);

        Handle<T>(handler);

        return this;
    }

    public int Handle(params string[] args)
    {
        try
        {
            Handle<HelpCommand>(Help);

            var parser = new CommandParser(_parsers);
            var command = parser.Parse(args);

            _handlers[command.GetType()](command);

            return Convert.ToInt32(command is HelpCommand);
        }
        catch (Exception e)
        {
            Error(e);

            return 1;
        }
    }

    private void Handle<T>(Action<T> handler)
    {
        _handlers[typeof(T)] = o =>
        {
            handler((T)o);
        };
    }

    private static void Error(Exception e)
    {
        Console.Error.WriteLine("Error:");

        IReadOnlyCollection<Exception> exceptions = e is AggregateException a
            ? a.InnerExceptions
            : new[] { e };

        var debugMode = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("DEBUG"));

        foreach (var exception in exceptions)
        {
            Console.Error.WriteLine(debugMode
                ? exception.ToString()
                : exception.Message);
        }
    }

    private static void Help(HelpCommand c)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  <command> <flags>");

        if (c.HelpTexts.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Help:");

            foreach (var helpText in c.HelpTexts)
            {
                Console.Error.WriteLine($"  {helpText.Topic}");
                Console.Error.WriteLine($"    {helpText.Info}");
            }
        }

        if (c.Errors.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(c.Errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in c.Errors)
            {
                Console.Error.WriteLine($"  {error}");
            }
        }

        Console.Error.WriteLine();
    }
}
