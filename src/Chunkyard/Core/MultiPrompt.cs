namespace Chunkyard.Core;

/// <summary>
/// A class to retrieve a user password using a set of <see cref="IPrompt"/>
/// instances.
/// </summary>
public class MultiPrompt : IPrompt
{
    private readonly IPrompt[] _prompts;

    public MultiPrompt(IEnumerable<IPrompt> prompts)
    {
        _prompts = prompts.ToArray();
    }

    public string NewPassword()
    {
        return First(prompt => prompt.NewPassword());
    }

    public string ExistingPassword()
    {
        return First(prompt => prompt.ExistingPassword());
    }

    private string First(Func<IPrompt, string?> passwordFunc)
    {
        foreach (var prompt in _prompts)
        {
            var password = passwordFunc(prompt);

            if (!string.IsNullOrEmpty(password))
            {
                return password;
            }
        }

        throw new ChunkyardException("Could not retrieve user password");
    }
}
