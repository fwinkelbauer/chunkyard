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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void EnvironmentPrompt_Returns_Empty_String_If_No_Or_Empty_Environment_Variable(string? password)
    {
        var prompt = new EnvironmentPrompt();

        Environment.SetEnvironmentVariable(
            EnvironmentPrompt.PasswordVariable,
            password);

        Assert.Empty(prompt.NewPassword());
        Assert.Empty(prompt.ExistingPassword());
    }
}
