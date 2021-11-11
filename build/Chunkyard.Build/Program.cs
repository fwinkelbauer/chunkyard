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
        var optionTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
            .ToArray();

        Parser.Default.ParseArguments(args, optionTypes)
            .WithParsed<CleanOptions>(Commands.Clean)
            .WithParsed<BuildOptions>(Commands.Build)
            .WithParsed<TestOptions>(Commands.Test)
            .WithParsed<CiOptions>(Commands.Ci)
            .WithParsed<PublishOptions>(Commands.Publish)
            .WithParsed<ReleaseOptions>(_ => Commands.Release())
            .WithParsed<FmtOptions>(_ => Commands.Fmt())
            .WithNotParsed(_ => Environment.ExitCode = 1);
    }
}
