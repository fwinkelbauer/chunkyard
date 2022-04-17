namespace Chunkyard.Core;

/// <summary>
/// A class to retrieve a user password using a set of <see cref="IPrompt"/>
/// implementations.
/// </summary>
public class Prompt
{
    private readonly IPrompt[] _prompts;

    public Prompt(IEnumerable<IPrompt> prompts)
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

            if (password != null)
            {
                return password;
            }
        }

        throw new ChunkyardException("Could not retrieve user password");
    }
}
