namespace Chunkyard.Tests.Infrastructure;

public static class RepositoryTests
{
    [Fact]
    public static void MemoryUriRepository_Can_Read_Write()
    {
        UriRepository_Can_Read_Write(new MemoryRepository<Uri>());
    }

    [Fact]
    public static void MemoryIntRepository_Can_Read_Write()
    {
        IntRepository_Can_Read_Write(new MemoryRepository<int>());
    }

    [Fact]
    public static void FileUriRepository_Can_Read_Write()
    {
        using var directory = new DisposableDirectory();
        var repository = FileRepository.CreateUriRepository(
            directory.Name);

        UriRepository_Can_Read_Write(repository);
    }

    [Fact]
    public static void FileIntRepository_Can_Read_Write()
    {
        using var directory = new DisposableDirectory();
        var repository = FileRepository.CreateIntRepository(
            directory.Name);

        IntRepository_Can_Read_Write(repository);
    }

    private static void UriRepository_Can_Read_Write(
        IRepository<Uri> repository)
    {
        var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var uri1 = new Uri("sha256://aa");
        var uri2 = new Uri("sha256://bb");

        Assert.Empty(repository.ListKeys());

        repository.StoreValue(uri1, expectedBytes);
        repository.StoreValue(uri2, expectedBytes);

        Assert.Equal(
            new[] { uri1, uri2 },
            repository.ListKeys().OrderBy(u => u.AbsoluteUri));

        Assert.True(repository.ValueExists(uri1));
        Assert.True(repository.ValueExists(uri2));

        Assert.Equal(expectedBytes, repository.RetrieveValue(uri1));
        Assert.Equal(expectedBytes, repository.RetrieveValue(uri2));

        repository.RemoveValue(uri1);
        repository.RemoveValue(uri2);

        Assert.Empty(repository.ListKeys());
        Assert.False(repository.ValueExists(uri1));
        Assert.False(repository.ValueExists(uri2));
    }

    private static void IntRepository_Can_Read_Write(
        IRepository<int> repository)
    {
        var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Empty(repository.ListKeys());

        repository.StoreValue(0, expectedBytes);
        repository.StoreValue(1, expectedBytes);

        Assert.Equal(
            new[] { 0, 1 },
            repository.ListKeys().OrderBy(i => i));

        Assert.True(repository.ValueExists(0));
        Assert.True(repository.ValueExists(1));

        Assert.Equal(expectedBytes, repository.RetrieveValue(0));
        Assert.Equal(expectedBytes, repository.RetrieveValue(1));

        repository.RemoveValue(0);
        repository.RemoveValue(1);

        Assert.Empty(repository.ListKeys());
        Assert.False(repository.ValueExists(0));
        Assert.False(repository.ValueExists(1));
    }
}
