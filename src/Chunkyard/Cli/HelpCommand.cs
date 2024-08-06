namespace Chunkyard.Cli;

/// <summary>
/// A class to encapsulate usage and error information when dealing with command
/// line arguments.
/// </summary>
public sealed class HelpCommand
{
    public HelpCommand(
        IReadOnlyDictionary<string, string> infos,
        IReadOnlyCollection<string> errors)
    {
        Infos = infos;
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string> Infos { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public override bool Equals(object? obj)
    {
        return obj is HelpCommand other
            && Infos.SequenceEqual(other.Infos)
            && Errors.SequenceEqual(other.Errors);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Infos, Errors);
    }
}
