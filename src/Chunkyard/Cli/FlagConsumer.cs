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
        out string[] list,
        string[]? defaultList = null)
    {
        if (defaultList != null)
        {
            var line = defaultList.Length == 0
                ? "<empty>"
                : string.Join(", ", defaultList);

            info = $"{info}. Default: {line}";
        }

        _help.AddFlag(flag, info);

        if (_flags.Remove(flag, out var value))
        {
            list = value.ToArray();
            return true;
        }
        else if (defaultList != null)
        {
            list = defaultList;
            return true;
        }
        else
        {
            _help.AddError($"Missing mandatory flag: {flag}");
            list = Array.Empty<string>();
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

        value = "";

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

        return false;
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
            _ = _flags.Remove(flag);
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

    public bool HelpNeeded(out HelpCommand help)
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

        return (helpRequested || help.Errors.Count > 0);
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
        var defaultStringValue = defaultValue == null
            ? null
            : convertTo(defaultValue.Value);

        value = default;

        if (TryString(flag, info, out var stringValue, defaultStringValue))
        {
            if (check(stringValue))
            {
                value = convertFrom(stringValue);
                return true;
            }
            else
            {
                _help.AddError($"Invalid value for flag: {flag}");
            }
        }

        return false;
    }
}
