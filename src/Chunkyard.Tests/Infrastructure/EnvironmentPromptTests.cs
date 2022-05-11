namespace Chunkyard.Tests.Infrastructure;

public static class EnvironmentPromptTests
{
    [Fact]
    public static void EnvironmentPrompt_Returns_Environment_Variable()
    {
        var password = "super-secret";
        var prompt = new EnvironmentPrompt();

        Environment.SetEnvironmentVariable(
            EnvironmentPrompt.PasswordVariable,
            password);

        Assert.Equal(password, prompt.NewPassword());
        Assert.Equal(password, prompt.ExistingPassword());
    }

    [Fact]
    public static void EnvironmentPrompt_Returns_Empty_String_If_No_Environment_Variable()
    {
        var prompt = new EnvironmentPrompt();

        Environment.SetEnvironmentVariable(
            EnvironmentPrompt.PasswordVariable,
            null);

        Assert.Empty(prompt.NewPassword());
        Assert.Empty(prompt.ExistingPassword());
    }
}
