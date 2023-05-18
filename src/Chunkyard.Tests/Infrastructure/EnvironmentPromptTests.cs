namespace Chunkyard.Tests.Infrastructure;

public static class EnvironmentPromptTests
{
    [Fact]
    public static void EnvironmentPrompt_Returns_Environment_Variable()
    {
        var password = "super-secret";
        var prompt = new EnvironmentPrompt();

        using var environment = CreateEnvironment(password);

        Assert.Equal(password, prompt.NewPassword());
        Assert.Equal(password, prompt.ExistingPassword());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void EnvironmentPrompt_Throws_If_No_Or_Empty_Environment_Variable(string? password)
    {
        var prompt = new EnvironmentPrompt();

        using var environment = CreateEnvironment(password);

        Assert.Throws<InvalidOperationException>(
            () => prompt.NewPassword());

        Assert.Throws<InvalidOperationException>(
            () => prompt.ExistingPassword());
    }

    private static DisposableEnvironment CreateEnvironment(string? password)
    {
        return new DisposableEnvironment(
            new Dictionary<string, string?>
            {
                { EnvironmentPrompt.PasswordVariable, password },
            });
    }
}
