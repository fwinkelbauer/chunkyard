using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Chunkyard.Core;
using Chunkyard.Tests.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class SnapshotStoreTests
    {
        [Fact]
        public static void AppendSnapshot_Creates_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var contentReferences = snapshot.ContentReferences;

            Assert.Equal(0, logPosition);
            Assert.Equal(
                new[] { "some content" },
                contentReferences.Select(c => c.Name));
        }

        [Fact]
        public static void AppendSnapshot_Detects_Previous_Snapshot_To_Deduplicate_Encrypted_Content()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition1 = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var logPosition2 = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var snapshot1 = snapshotStore.GetSnapshot(logPosition1);
            var snapshot2 = snapshotStore.GetSnapshot(logPosition2);

            Assert.Equal(
                snapshot1.ContentReferences,
                snapshot2.ContentReferences);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Even_If_Empty()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                Array.Empty<string>(),
                OpenStream(),
                DateTime.Now);

            Assert.Equal(0, logPosition);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Without_New_Data()
        {
            var snapshotStore = CreateSnapshotStore();
            var contents = new[] { "some content" };

            snapshotStore.AppendSnapshot(
                contents,
                OpenStream(),
                DateTime.Now);

            var logPosition = snapshotStore.AppendSnapshot(
                contents,
                OpenStream(),
                DateTime.Now);

            Assert.Equal(1, logPosition);
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_LogPositions()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            Assert.Equal(
                snapshotStore.GetSnapshot(logPosition),
                snapshotStore.GetSnapshot(-1));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Ok()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            Assert.True(snapshotStore.CheckSnapshotExists(logPosition));
            Assert.True(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Missing()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            repository.RemoveUris(
                repository.ListUris());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(logPosition));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Invalid()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            repository.CorruptUris(
                repository.ListUris());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(logPosition));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Missing_Content()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var contentUris = snapshot.ContentReferences
                .Select(contentReference => contentReference.Chunks)
                .SelectMany(chunk => chunk)
                .Select(chunk => chunk.ContentUri);

            repository.RemoveUris(contentUris);

            Assert.False(snapshotStore.CheckSnapshotExists(logPosition));
            Assert.False(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Invalid_Content()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var contentUris = snapshot.ContentReferences
                .Select(contentReference => contentReference.Chunks)
                .SelectMany(chunk => chunk)
                .Select(chunk => chunk.ContentUri);

            repository.CorruptUris(contentUris);

            Assert.True(snapshotStore.CheckSnapshotExists(logPosition));
            Assert.False(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void Restore_Writes_Content_To_Streams()
        {
            var snapshotStore = CreateSnapshotStore();
            var content = new byte[] { 0x11, 0x22, 0x33, 0x44 };

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(content),
                DateTime.Now);

            var actualContentName = "";
            using var writeStream = new MemoryStream();

            Stream openWrite(string s)
            {
                actualContentName = s;
                return writeStream;
            }

            snapshotStore.RestoreSnapshot(logPosition, "", openWrite);

            Assert.Equal("some content", actualContentName);
            Assert.Equal(content, writeStream.ToArray());
        }

        [Fact]
        public static void ShowSnapshot_Lists_Content()
        {
            var snapshotStore = CreateSnapshotStore();
            var contents = new[] { "some content" };

            var logPosition = snapshotStore.AppendSnapshot(
                contents,
                OpenStream(),
                DateTime.Now);

            Assert.Equal(
                contents,
                snapshotStore.ShowSnapshot(logPosition).Select(c => c.Name));
        }

        [Fact]
        public static void GarbageCollect_Keeps_Used_Uris()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            snapshotStore.GarbageCollect();

            Assert.True(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void GarbageCollect_Removes_Unused_Uris()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(
                repository);

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            repository.RemoveFromLog(logPosition);

            snapshotStore.GarbageCollect();

            Assert.Empty(repository.ListUris());
        }

        [Fact]
        public static void CopySnapshots_Throws_If_No_Overlap()
        {
            var sourceRepository = new MemoryRepository();
            var sourceSnapshotStore = CreateSnapshotStore(sourceRepository);

            var destinationRepository = new MemoryRepository();
            var destinationSnapshotStore = CreateSnapshotStore(
                destinationRepository);

            var firstLogPosition = sourceSnapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            sourceSnapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            sourceRepository.RemoveFromLog(firstLogPosition);

            destinationSnapshotStore.AppendSnapshot(
                new[] { "some other content" },
                OpenStream(),
                DateTime.Now);

            Assert.Throws<ChunkyardException>(
                () => sourceSnapshotStore.CopySnapshots(destinationRepository));
        }

        [Fact]
        public static void CopySnapshots_Throws_If_Snapshots_Dont_Match()
        {
            var sourceSnapshotStore = CreateSnapshotStore();

            var destinationRepository = new MemoryRepository();
            var destinationSnapshotStore = CreateSnapshotStore(
                destinationRepository);

            sourceSnapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            destinationSnapshotStore.AppendSnapshot(
                new[] { "some other content" },
                OpenStream(),
                DateTime.Now);

            Assert.Throws<ChunkyardException>(
                () => sourceSnapshotStore.CopySnapshots(destinationRepository));
        }

        [Fact]
        public static void CopySnapshots_Copies_Missing_Data()
        {
            var sourceRepository = new MemoryRepository();
            var sourceSnapshotStore = CreateSnapshotStore(sourceRepository);
            var destinationRepository = new MemoryRepository();

            sourceSnapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            sourceSnapshotStore.CopySnapshots(destinationRepository);

            Assert.Equal(
                sourceRepository.ListUris(),
                destinationRepository.ListUris());

            Assert.Equal(
                sourceRepository.ListLogPositions(),
                destinationRepository.ListLogPositions());
        }

        private static Func<string, Stream> OpenStream(
            byte[]? content = null)
        {
            return (s) => new MemoryStream(
                content == null
                    ? new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }
                    : content);
        }

        private static SnapshotStore CreateSnapshotStore(
            IRepository? repository = null)
        {
            repository = repository ?? new MemoryRepository();

            return new SnapshotStore(
                new ContentStore(
                    repository,
                    new FastCdc(),
                    HashAlgorithmName.SHA256,
                    new StaticPrompt()),
                repository);
        }
    }
}
