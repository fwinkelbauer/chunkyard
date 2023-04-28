namespace Chunkyard.Tests.Infrastructure;

internal sealed class DisposableEnvironment : IDisposable
{
    private readonly Dictionary<string, string?> _initialVariables;

    public DisposableEnvironment(
        IReadOnlyDictionary<string, string?> variables)
    {
        _initialVariables = new();

        foreach (var (variable, value) in variables)
        {
            _initialVariables[variable] = Environment.GetEnvironmentVariable(
                variable);

            Environment.SetEnvironmentVariable(variable, value);
        }
    }

    public void Dispose()
    {
        foreach (var (variable, value) in _initialVariables)
        {
            Environment.SetEnvironmentVariable(variable, value);
        }
    }
}
