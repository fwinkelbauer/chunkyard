namespace Chunkyard.CommandLine;

/// <summary>
/// A stateful helper class used to parse commands.
/// </summary>
public sealed class FlagConsumer
{
    private readonly Dictionary<string, IReadOnlyCollection<string>> _availableFlags;
    private readonly Dictionary<string, IReadOnlyCollection<string>> _consumedFlags;
    private readonly HelpCommandBuilder _help;

    public FlagConsumer(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> flags,
        HelpCommandBuilder help)
    {
        _availableFlags = new(flags);
        _consumedFlags = new();
        _help = help;
    }

    public bool TryStrings(
        string flag,
        string info,
        out string[] values,
        string[]? defaultValues = null)
    {
        if (defaultValues != null)
        {
            var line = defaultValues.Length == 0
                ? "<empty>"
                : string.Join(", ", defaultValues);

            info = $"{info}. Default: {line}";
        }

        _help.AddFlag(flag, info);

        if (_availableFlags.Remove(flag, out var value))
        {
            values = value.ToArray();
            _consumedFlags[flag] = value;
            return true;
        }
        else if (_consumedFlags.TryGetValue(flag, out var consumed))
        {
            values = consumed.ToArray();
            return true;
        }
        else if (defaultValues != null)
        {
            values = defaultValues;
            return true;
        }
        else
        {
            _help.AddError($"Missing mandatory flag: {flag}");
            values = Array.Empty<string>();
            return false;
        }
    }

    public bool TryString(
        string flag,
        string info,
        out string value,
        string? defaultValue = null)
    {
        var defaultList = defaultValue == null
            ? null
            : new[] { defaultValue };

        if (TryStrings(flag, info, out var list, defaultList))
        {
            if (list.Length > 0)
            {
                value = list[^1];
                return true;
            }
            else
            {
                _help.AddError($"Empty flag: {flag}");
            }
        }

        value = "";
        return false;
    }

    public bool TryValues<T>(
        string flag,
        string info,
        out T[] values,
        Func<string, T> fromString,
        Func<T, string> toString,
        T[]? defaultValues = null)
    {
        var defaultStrings = defaultValues?.Select(toString).ToArray();

        try
        {
            if (TryStrings(flag, info, out var strings, defaultStrings))
            {
                values = strings.Select(fromString).ToArray();
                return true;
            }
        }
        catch (Exception)
        {
            _help.AddError($"Invalid value for flag: {flag}");
        }

        values = Array.Empty<T>();
        return false;
    }

    public bool TryValue<T>(
        string flag,
        string info,
        out T value,
        Func<string, T> fromString,
        Func<T, string> toString,
        T? defaultValue = null)
        where T : struct
    {
        var defaultString = defaultValue.HasValue
            ? toString(defaultValue.Value)
            : null;

        try
        {
            if (TryString(flag, info, out var str, defaultString))
            {
                value = fromString(str);
                return true;
            }
        }
        catch (Exception)
        {
            _help.AddError($"Invalid value for flag: {flag}");
        }

        value = default;
        return false;
    }

    public bool TryBool(string flag, string info, out bool value)
    {
        if (_availableFlags.TryGetValue(flag, out var list)
            && list.Count == 0)
        {
            _ = _availableFlags.Remove(flag);
            _consumedFlags[flag] = list;
            _help.AddFlag(flag, info);

            value = true;
            return true;
        }
        else if (_consumedFlags.TryGetValue(flag, out var consumed)
            && consumed.Count == 0)
        {
            _help.AddFlag(flag, info);

            value = true;
            return true;
        }
        else
        {
            return TryValue(
                flag,
                info,
                out value,
                bool.Parse,
                b => b.ToString(),
                false);
        }
    }

    public bool HelpNeeded(out HelpCommand help)
    {
        var helpRequested = TryBool("--help", "Print usage information", out var h)
            && h;

        if (_availableFlags.Count != 0)
        {
            foreach (var flag in _availableFlags.Keys)
            {
                _help.AddError($"Unknown flag: {flag}");
            }

            _availableFlags.Clear();
        }

        help = _help.Build();

        return helpRequested || help.Errors.Count > 0;
    }
}
