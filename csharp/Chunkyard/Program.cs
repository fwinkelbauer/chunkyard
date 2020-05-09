using System;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard
{
    public static class Program
    {
        public static void Main()
        {
            var snapshotBuilder = SnapshotBuilder.Create(
                new ConsolePrompt(),
                new ContentStore(
                    new FileRepository(Path.GetFullPath("./test"))),
                HashAlgorithmName.SHA256,
                2 * 1024 * 1024,
                4 * 1024 * 1024,
                8 * 1024 * 1024);

            //var file = "foo2.txt";
            //using var stream = File.OpenRead("foo2.txt");
            //snapshotBuilder.AddContent(stream, file);
            //snapshotBuilder.WriteSnapshot(DateTime.Now);

            snapshotBuilder.RestoreSnapshot(
                2,
                (contentName) =>
                {
                    var file = Path.Combine("test-restore", contentName);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    return new FileStream(
                        file,
                        FileMode.CreateNew,
                        FileAccess.Write);
                },
                ".*");
        }
    }
}
