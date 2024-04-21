namespace Chunkyard.Cli;

/// <summary>
/// A stateful helper class used by instances of <see cref="ICommandParser"/>.
/// </summary>
public sealed class FlagConsumer
{
    private readonly Dictionary<string, IReadOnlyCollection<string>> _flags;
    private readonly Dictionary<string, string> _infos;
    private readonly HashSet<string> _errors;

    public FlagConsumer(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> flags)
    {
        _flags = new(flags);
        _infos = new();
        _errors = new();
    }

    public HelpCommand Help => new(
        new Dictionary<string, string>(_infos),
        new HashSet<string>(_errors));

    public bool TryStrings(
        string flag,
        string info,
        out IReadOnlyCollection<string> list)
    {
        if (_flags.TryGetValue(flag, out var value))
        {
            _flags.Remove(flag);
            list = value;
        }
        else
        {
            list = Array.Empty<string>();
        }

        _infos[flag] = info;

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
            && list.Any())
        {
            parsed = list.Last();
        }
        else if (defaultValue != null)
        {
            parsed = defaultValue;
        }
        else
        {
            _errors.Add($"Missing mandatory flag: {flag}");
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
            _infos[flag] = info;

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
        return TryStruct(
            flag,
            info,
            out value,
            s => Enum.TryParse<T>(s, true, out _),
            s => Enum.Parse<T>(s, true),
            e => Enum.GetName(typeof(T), e)!,
            defaultValue);
    }

    public bool TryEmpty()
    {
        var empty = _flags.Count == 0;

        if (!empty)
        {
            _errors.UnionWith(_flags.Keys.Select(k => $"Extra flag: {k}"));
            _flags.Clear();
        }

        return empty;
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
                _errors.Add($"Invalid value: {flag}");
            }
        }

        value = parsed ?? default;

        return parsed != null;
    }
}
