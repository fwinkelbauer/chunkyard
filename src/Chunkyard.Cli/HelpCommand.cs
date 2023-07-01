namespace Chunkyard.Cli;

public sealed class HelpCommand : ICommand
{
    private readonly IReadOnlyCollection<HelpText> _helpTexts;
    private readonly IReadOnlyCollection<string> _errors;

    public HelpCommand(
        IReadOnlyCollection<HelpText> helpTexts,
        IReadOnlyCollection<string> errors)
    {
        _helpTexts = helpTexts;
        _errors = errors;
    }

    public void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"  <command> <flags>");
        Console.WriteLine($"  <command> --help");
        Console.WriteLine($"  help");

        if (_helpTexts.Any())
        {
            Console.WriteLine();
            Console.WriteLine($"Commands:");

            foreach (var helpText in _helpTexts)
            {
                Console.WriteLine($"  {helpText.Topic}");
                Console.WriteLine($"    {helpText.Info}");
            }
        }

        if (_errors.Any())
        {
            Console.WriteLine();
            Console.WriteLine(_errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in _errors)
            {
                Console.WriteLine($"  {error}");
            }
        }

        Console.WriteLine();
        Environment.ExitCode = 1;
    }
}
