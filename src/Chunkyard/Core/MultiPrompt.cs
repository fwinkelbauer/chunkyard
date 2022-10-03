namespace Chunkyard.Core;

/// <summary>
/// A class to retrieve a user password using a set of <see cref="IPrompt"/>
/// instances.
/// </summary>
public sealed class MultiPrompt : IPrompt
{
    private readonly IPrompt[] _prompts;

    public MultiPrompt(params IPrompt[] prompts)
    {
        _prompts = prompts;
    }

    public string NewPassword()
    {
        return First(prompt => prompt.NewPassword());
    }

    public string ExistingPassword()
    {
        return First(prompt => prompt.ExistingPassword());
    }

    private string First(Func<IPrompt, string> passwordFunc)
    {
        return _prompts
            .Select(passwordFunc)
            .First(password => !string.IsNullOrEmpty(password));
    }
}
