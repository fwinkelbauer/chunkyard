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
