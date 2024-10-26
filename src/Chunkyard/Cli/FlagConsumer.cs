namespace Chunkyard.Cli;

/// <summary>
/// A stateful helper class used by instances of <see cref="ICommandParser"/>.
/// </summary>
public sealed class FlagConsumer
{
    private readonly Dictionary<string, IReadOnlyCollection<string>> _flags;
    private readonly HelpCommandBuilder _help;

    public FlagConsumer(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> flags,
        HelpCommandBuilder help)
    {
        _flags = new(flags);
        _help = help;
    }

    public bool TryStrings(
        string flag,
        string info,
        out string[] list)
    {
        list = _flags.Remove(flag, out var value)
            ? value.ToArray()
            : Array.Empty<string>();

        _help.AddFlag(flag, info);

        return true;
    }

    public bool TryString(
        string flag,
        string info,
        out string value,
        string? defaultValue = null)
    {
        string? parsed = null;

        if (!string.IsNullOrEmpty(defaultValue))
        {
            info = $"{info}. Default: {defaultValue}";
        }

        if (TryStrings(flag, info, out var list)
            && list.Length > 0)
        {
            parsed = list.Last();
        }
        else if (defaultValue != null)
        {
            parsed = defaultValue;
        }
        else
        {
            _help.AddError($"Missing mandatory flag: {flag}");
        }

        value = parsed ?? "";

        return parsed != null;
    }

    public bool TryInt(
        string flag,
        string info,
        out int value,
        int? defaultValue = null)
    {
        return TryStruct(
            flag,
            info,
            out value,
            s => int.TryParse(s, out _),
            int.Parse,
            i => i.ToString(),
            defaultValue);
    }

    public bool TryBool(string flag, string info, out bool value)
    {
        if (_flags.TryGetValue(flag, out var list)
            && list.Count == 0)
        {
            _flags.Remove(flag);
            _help.AddFlag(flag, info);

            value = true;
            return true;
        }
        else
        {
            return TryStruct(
                flag,
                info,
                out value,
                s => bool.TryParse(s, out _),
                bool.Parse,
                b => b.ToString(),
                false);
        }
    }

    public bool TryEnum<T>(
        string flag,
        string info,
        out T value,
        T? defaultValue = null)
        where T : struct
    {
        var names = string.Join(", ", Enum.GetNames(typeof(T)));

        return TryStruct(
            flag,
            $"{info}: {names}",
            out value,
            s => Enum.TryParse<T>(s, true, out _),
            s => Enum.Parse<T>(s, true),
            e => Enum.GetName(typeof(T), e)!,
            defaultValue);
    }

    public bool NoHelp(out HelpCommand help)
    {
        var helpRequested = TryBool("--help", "Print usage information", out var h)
            && h;

        if (_flags.Count != 0)
        {
            foreach (var flag in _flags.Keys)
            {
                _help.AddError($"Unknown flag: {flag}");
            }

            _flags.Clear();
        }

        help = _help.Build();

        return !(helpRequested || help.Errors.Count > 0);
    }

    private bool TryStruct<T>(
        string flag,
        string info,
        out T value,
        Func<string, bool> check,
        Func<string, T> convertFrom,
        Func<T, string> convertTo,
        T? defaultValue = null)
        where T : struct
    {
        T? parsed = null;

        var defaultStringValue = defaultValue == null
            ? null
            : convertTo(defaultValue.Value);

        if (TryString(flag, info, out var stringValue, defaultStringValue))
        {
            if (check(stringValue))
            {
                parsed = convertFrom(stringValue);
            }
            else
            {
                _help.AddError($"Invalid value: {flag}");
            }
        }

        value = parsed ?? default;

        return parsed != null;
    }
}
