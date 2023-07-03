namespace Chunkyard.Cli;

/// <summary>
/// A command which can be used to inform a user on how the command line
/// interface can be used.
/// </summary>
public sealed class HelpCommand
{
    public HelpCommand(
        IReadOnlyCollection<HelpText> helpTexts,
        IReadOnlyCollection<string> errors)
    {
        HelpTexts = helpTexts;
        Errors = errors;
    }

    public IReadOnlyCollection<HelpText> HelpTexts { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public string ToText()
    {
        var builder = new StringBuilder();

        Console.WriteLine();
        builder.AppendLine("Usage:");
        builder.AppendLine("  <command> <flags>");
        builder.AppendLine("  <command> --help");
        builder.AppendLine("  help");

        if (HelpTexts.Any())
        {
            builder.AppendLine();
            builder.AppendLine("Help:");

            foreach (var helpText in HelpTexts)
            {
                builder.AppendLine($"  {helpText.Topic}");
                builder.AppendLine($"    {helpText.Info}");
            }
        }

        if (Errors.Any())
        {
            builder.AppendLine();
            builder.AppendLine(Errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in Errors)
            {
                builder.AppendLine($"  {error}");
            }
        }

        return builder.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is HelpCommand other
            && HelpTexts.SequenceEqual(other.HelpTexts)
            && Errors.SequenceEqual(other.Errors);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HelpTexts, Errors);
    }
}
