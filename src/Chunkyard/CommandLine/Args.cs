namespace Chunkyard.CommandLine;

/// <summary>
/// A structured form of input arguments.
///
/// Command line arguments are expected to have the following shape:
///
/// [command] [flags]
///
/// e.g. my-command --some-flag param1 param2 --another-flag
/// </summary>
public sealed class Args
{
    public Args(
        string command,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> flags)
    {
        Command = command;
        Flags = flags;
    }

    public string Command { get; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Flags { get; }

    public override bool Equals(object? obj)
    {
        return obj is Args other
            && Command.Equals(other.Command)
            && Flags.Keys.SequenceEqual(other.Flags.Keys)
            && Flags.Values.SelectMany(v => v)
                .SequenceEqual(other.Flags.Values.SelectMany(v => v));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Command, Flags);
    }

    public static Args? Parse(params string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var command = "";
        var currentFlag = "";
        var flags = new Dictionary<string, List<string>>();

        foreach (var token in args)
        {
            if (token.StartsWith('-')
                && !int.TryParse(token, out _))
            {
                currentFlag = token;

                if (!flags.ContainsKey(currentFlag))
                {
                    flags.Add(currentFlag, new List<string>());
                }
            }
            else if (string.IsNullOrEmpty(currentFlag))
            {
                command = $"{command} {token}".Trim();
            }
            else
            {
                flags[currentFlag].Add(token);
            }
        }

        var flagsCasted = flags.ToDictionary(
            pair => pair.Key,
            IReadOnlyCollection<string> (pair) => pair.Value);

        return new Args(command, flagsCasted);
    }
}
