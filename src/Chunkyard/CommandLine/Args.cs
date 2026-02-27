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
public sealed record Args(
    string Command,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> Flags)
{
    public bool Equals(Args? other)
    {
        return other is not null
            && Command.Equals(other.Command)
            && Flags.Keys.SequenceEqual(other.Flags.Keys)
            && Flags.All(kv => kv.Value.SequenceEqual(other.Flags[kv.Key]));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Command);

        foreach (var (key, values) in Flags)
        {
            hash.Add(key);

            foreach (var value in values)
            {
                hash.Add(value);
            }
        }

        return hash.ToHashCode();
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
                flags.TryAdd(currentFlag, new List<string>());
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
