namespace Chunkyard.Tests.Infrastructure;

public static class DotEnvTests
{
    [Fact]
    public static void Populate_Loads_All_Env_Files_Up_To_Root()
    {
        var firstVariable = "FIRST-VARIABLE";
        var firstText = "first-text";

        var secondVariable = "SECOND-VARIABLE";
        var secondText = "second-text";

        var thirdVariable = "THIRD-VARIABLE";
        var thirdText = "third-text";

        var variables = new[] { firstVariable, secondVariable, thirdVariable };
        var texts = new[] { firstText, secondText, thirdText };

        using var directory = new DisposableDirectory();
        using var subDirectory = new DisposableDirectory(directory.Name);

        subDirectory.Create();

        File.WriteAllLines(
            Path.Combine(directory.Name, DotEnv.FileName),
            new[]
            {
                $"{firstVariable}=not-relevant-text",
                $"{secondVariable}={secondText}"
            });

        File.WriteAllLines(
            Path.Combine(subDirectory.Name, DotEnv.FileName),
            new[]
            {
                $"{firstVariable}={firstText}",
                $"{thirdVariable}={thirdText}"
            });

        DotEnv.Populate(subDirectory.Name);

        Assert.Equal(
            texts,
            variables.Select(Environment.GetEnvironmentVariable));
    }
}
