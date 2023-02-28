namespace Chunkyard.Benchmarks;

public class ProgramBenchmarks
{
    private readonly string _repositoryDirectory = ".benchmark-repository";

    private SnapshotStore? _snapshotStore;
    private IBlobSystem? _blobSystem;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Directory.SetCurrentDirectory(
            ProcessUtils.RunQuery("git", "rev-parse --show-toplevel"));

        _blobSystem = new FileBlobSystem(
            ProcessUtils.RunQuery("git", "ls-files")
                .Split(Environment.NewLine),
            Fuzzy.Default);
    }

    [IterationSetup]
    public void Setup()
    {
        _snapshotStore = new SnapshotStore(
            new FileRepository(_repositoryDirectory),
            new FastCdc(),
            new DummyProbe(),
            new RealClock(),
            new DummyPrompt("super-secret"));
    }

    [Benchmark]
    public void Store()
    {
        _snapshotStore!.StoreSnapshot(_blobSystem!);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        Directory.Delete(_repositoryDirectory, true);
    }
}
