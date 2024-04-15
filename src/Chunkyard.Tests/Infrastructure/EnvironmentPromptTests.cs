namespace Chunkyard.Tests.Infrastructure;

public static class EnvironmentPromptTests
{
    [Fact]
    public static void EnvironmentPrompt_Returns_Environment_Variable()
    {
        var repositoryId = "some-repository";
        var password = "super-secret";
        var prompt = new EnvironmentPrompt();

        using var environment = CreateEnvironment(password);

        Assert.Equal(password, prompt.NewPassword(repositoryId));
        Assert.Equal(password, prompt.ExistingPassword(repositoryId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void EnvironmentPrompt_Throws_If_No_Or_Empty_Environment_Variable(string? password)
    {
        var repositoryId = "some-repository";
        var prompt = new EnvironmentPrompt();

        using var environment = CreateEnvironment(password);

        Assert.Throws<InvalidOperationException>(
            () => prompt.NewPassword(repositoryId));

        Assert.Throws<InvalidOperationException>(
            () => prompt.ExistingPassword(repositoryId));
    }

    private static DisposableEnvironment CreateEnvironment(string? password)
    {
        return new DisposableEnvironment(
            new Dictionary<string, string?>
            {
                { EnvironmentPrompt.PasswordVariable, password }
            });
    }
}
