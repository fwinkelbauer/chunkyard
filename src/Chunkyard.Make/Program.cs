namespace Chunkyard.Make;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            ProcessArguments(args);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error: {e.Message}");
            Environment.ExitCode = 1;
        }

        return Environment.ExitCode;
    }

    private static void ProcessArguments(string[] args)
    {
        Parser.Default.ParseArguments<CleanOptions, BuildOptions, PublishOptions, ReleaseOptions, FmtOptions, OutdatedOptions>(args)
            .WithParsed<CleanOptions>(_ => Commands.Clean())
            .WithParsed<BuildOptions>(_ => Commands.Build())
            .WithParsed<PublishOptions>(_ => Commands.Publish())
            .WithParsed<ReleaseOptions>(_ => Commands.Release())
            .WithParsed<FmtOptions>(_ => Commands.Fmt())
            .WithParsed<OutdatedOptions>(_ => Commands.Outdated())
            .WithNotParsed(_ => Environment.ExitCode = 1);
    }
}
