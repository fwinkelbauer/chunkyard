namespace Chunkyard.Cli;

public sealed class ArgConsumer
{
    private readonly Dictionary<string, IReadOnlyCollection<string>> _flags;
    private readonly List<HelpText> _helpTexts;
    private readonly List<string> _errors;

    public ArgConsumer(Arg arg)
    {
        _flags = new Dictionary<string, IReadOnlyCollection<string>>(arg.Flags);
        _helpTexts = new List<HelpText>();
        _errors = new List<string>();
    }

    public IReadOnlyCollection<HelpText> HelpTexts => _helpTexts;

    public IReadOnlyCollection<string> Errors => _errors;

    public bool TryList(
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

        _helpTexts.Add(new HelpText(flag, info));

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

        if (TryList(flag, info, out var list)
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
            s => int.Parse(s),
            i => i.ToString(),
            defaultValue);
    }

    public bool TryBool(string flag, string info, out bool value)
    {
        if (_flags.TryGetValue(flag, out var list)
            && list.Count == 0)
        {
            _flags.Remove(flag);

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
                s => bool.Parse(s),
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
            s => Enum.TryParse<T>(s, out _),
            s => Enum.Parse<T>(s),
            e => Enum.GetName(typeof(T), e)!,
            defaultValue);
    }

    public bool IsConsumed()
    {
        var consumed = _flags.Count == 0;

        if (!consumed)
        {
            _errors.AddRange(_flags.Keys.Select(k => $"Extra flag: {k}"));
        }

        return consumed;
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

        string? defaultStringValue = defaultValue == null
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

        value = parsed != null
            ? parsed.Value
            : default;

        return parsed != null;
    }
}
