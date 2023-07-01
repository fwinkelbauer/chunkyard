namespace Chunkyard.Cli;

public sealed class Arg
{
    public Arg(
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
        return obj is Arg other
            && Command.Equals(other.Command)
            && Flags.Keys.SequenceEqual(other.Flags.Keys)
            && Flags.Values.SelectMany(v => v)
                .SequenceEqual(other.Flags.Values.SelectMany(v => v));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Command, Flags);
    }

    public static Arg? Parse(params string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var command = args[0];
        var currentFlag = "";
        var flags = new Dictionary<string, List<string>>();

        for (var i = 1; i < args.Length; i++)
        {
            var token = args[i];

            if (token.StartsWith("-")
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
                return null;
            }
            else
            {
                flags[currentFlag].Add(token);
            }
        }

        var flagsCasted = flags.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<string>)pair.Value);

        return new Arg(command, flagsCasted);
    }
}
