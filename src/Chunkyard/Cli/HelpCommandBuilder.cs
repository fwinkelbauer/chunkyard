namespace Chunkyard.Cli;

/// <summary>
/// A helper class to create instances of <see cref="HelpCommand"/>.
/// </summary>
public sealed class HelpCommandBuilder
{
    private readonly string _headline;
    private readonly Dictionary<string, string> _commandInfos;
    private readonly Dictionary<string, string> _flagInfos;
    private readonly HashSet<string> _errors;

    public HelpCommandBuilder(string headline)
    {
        _headline = headline;
        _commandInfos = new();
        _flagInfos = new();
        _errors = new();
    }

    public void AddCommand(string name, string info)
    {
        _commandInfos.Add(name, info);
    }

    public void AddFlag(string name, string info)
    {
        _flagInfos.Add(name, info);
    }

    public void AddError(string error)
    {
        _ = _errors.Add(error);
    }

    public HelpCommand Build()
    {
        return new HelpCommand(
            _headline,
            _commandInfos,
            _flagInfos,
            _errors);
    }
}
