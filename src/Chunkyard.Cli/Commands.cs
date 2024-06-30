namespace Chunkyard.Cli;

/// <summary>
/// A command which can be used to inform a user on how the command line
/// interface can be used.
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

/// <summary>
/// A command to show the current version of this application.
/// </summary>
public sealed class VersionCommand;
