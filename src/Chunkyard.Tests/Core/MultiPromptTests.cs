namespace Chunkyard.Tests.Core;

public static class MultiPromptTests
{
    public static TheoryData<MultiPrompt> TheoryData => new()
    {
        { new MultiPrompt(new[] { new DummyPrompt("") }) },
        { new MultiPrompt(Array.Empty<IPrompt>()) }
    };

    [Theory, MemberData(nameof(TheoryData))]
    public static void MultiPrompt_Returns_Empty_If_Prompts_Empty(MultiPrompt prompt)
    {
        Assert.Throws<InvalidOperationException>(
            () => prompt.NewPassword());

        Assert.Throws<InvalidOperationException>(
            () => prompt.ExistingPassword());
    }

    [Fact]
    public static void MultiPrompt_Returns_First_Non_Empty_Password()
    {
        var expectedPassword = "some-password";
        var prompt = new MultiPrompt(
            new[]
            {
                new DummyPrompt(""),
                new DummyPrompt(expectedPassword),
                new DummyPrompt("other-password")
            });

        Assert.Equal(expectedPassword, prompt.NewPassword());
        Assert.Equal(expectedPassword, prompt.ExistingPassword());
    }
}
