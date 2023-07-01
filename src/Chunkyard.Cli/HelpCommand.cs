namespace Chunkyard.Cli;

internal sealed record HelpText(string Topic, string Info);

/// <summary>
/// A command which can be used to inform a user on how the command line
/// interface can be used.
/// </summary>
public sealed class HelpCommand
{
    private readonly IReadOnlyCollection<HelpText> _helpTexts;
    private readonly IReadOnlyCollection<string> _errors;

    internal HelpCommand(
        IReadOnlyCollection<HelpText> helpTexts,
        IReadOnlyCollection<string> errors)
    {
        _helpTexts = helpTexts;
        _errors = errors;
    }

    public string ToText()
    {
        var builder = new StringBuilder();

        Console.WriteLine();
        builder.AppendLine("Usage:");
        builder.AppendLine($"  <command> <flags>");
        builder.AppendLine($"  <command> --help");
        builder.AppendLine($"  help");

        if (_helpTexts.Any())
        {
            builder.AppendLine();
            builder.AppendLine($"Help:");

            foreach (var helpText in _helpTexts)
            {
                builder.AppendLine($"  {helpText.Topic}");
                builder.AppendLine($"    {helpText.Info}");
            }
        }

        if (_errors.Any())
        {
            builder.AppendLine();
            builder.AppendLine(_errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in _errors)
            {
                builder.AppendLine($"  {error}");
            }
        }

        return builder.ToString();
    }
}
