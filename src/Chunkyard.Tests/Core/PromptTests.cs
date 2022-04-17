namespace Chunkyard.Tests.Core;

public static class PromptTests
{
    public static TheoryData<Prompt> TheoryData => new()
    {
        { new Prompt(new[] { new DummyPrompt(null) }) },
        { new Prompt(Array.Empty<IPrompt>()) }
    };

    [Theory, MemberData(nameof(TheoryData))]
    public static void Prompt_Throws_If_Null_Or_Empty(Prompt prompt)
    {
        Assert.Throws<ChunkyardException>(
            () => prompt.NewPassword());

        Assert.Throws<ChunkyardException>(
            () => prompt.ExistingPassword());
    }

    [Fact]
    public static void Prompt_Returns_Password()
    {
        var expectedPassword = "some-password";
        var prompt = new Prompt(
            new[]
            {
                new DummyPrompt(null),
                new DummyPrompt(expectedPassword),
                new DummyPrompt("some-password")
            });

        Assert.Equal(expectedPassword, prompt.NewPassword());
        Assert.Equal(expectedPassword, prompt.ExistingPassword());
    }
}
