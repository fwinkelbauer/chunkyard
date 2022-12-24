namespace Chunkyard.Build;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            ProcessArguments(args);
        }
        catch (Exception e)
        {
            WriteError(e.Message);
        }
    }

    private static void WriteError(string message)
    {
        Environment.ExitCode = 1;

        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
        }
        finally
        {
            Console.ResetColor();
        }
    }

    private static void ProcessArguments(string[] args)
    {
        Parser.Default.ParseArguments<CleanOptions, BuildOptions, PublishOptions, ReleaseOptions, FmtOptions, OutdatedOptions>(args)
            .WithParsed<CleanOptions>(_ => Commands.Clean())
            .WithParsed<BuildOptions>(Commands.Build)
            .WithParsed<PublishOptions>(_ => Commands.Publish())
            .WithParsed<ReleaseOptions>(_ => Commands.Release())
            .WithParsed<FmtOptions>(_ => Commands.Fmt())
            .WithParsed<OutdatedOptions>(_ => Commands.Outdated())
            .WithNotParsed(_ => Environment.ExitCode = 1);
    }
}
