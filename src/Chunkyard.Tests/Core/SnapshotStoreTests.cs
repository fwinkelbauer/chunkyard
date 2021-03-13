using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

            var expectedNames = new[] { "some content" };

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(expectedNames),
                DateTime.Now,
                OpenRead);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var blobReferences = snapshot.BlobReferences;

            Assert.Equal(0, logPosition);
            Assert.Equal(
                expectedNames,
                blobReferences.Select(c => c.Name));
        }

        [Fact]
        public static void AppendSnapshot_Detects_Previous_Snapshot_To_Deduplicate_Encrypted_Content()
        {
            var snapshotStore = CreateSnapshotStore();

            var blobs = CreateBlobs(new[] { "some content" });

            var logPosition1 = snapshotStore.AppendSnapshot(
                blobs,
                DateTime.Now,
                OpenRead);

            var logPosition2 = snapshotStore.AppendSnapshot(
                blobs,
                DateTime.Now,
                OpenRead);

            var snapshot1 = snapshotStore.GetSnapshot(logPosition1);
            var snapshot2 = snapshotStore.GetSnapshot(logPosition2);

            Assert.Equal(
                snapshot1.BlobReferences,
                snapshot2.BlobReferences);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Even_If_Empty()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(Array.Empty<string>()),
                DateTime.Now,
                OpenRead);

            Assert.Equal(0, logPosition);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Without_New_Data()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs(new[] { "some content" });

            snapshotStore.AppendSnapshot(
                blobs,
                DateTime.Now,
                OpenRead);

            var logPosition = snapshotStore.AppendSnapshot(
                blobs,
                DateTime.Now,
                OpenRead);

            Assert.Equal(1, logPosition);
        }

        [Fact]
        public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var expectedNames = new[] { "some content" };

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(expectedNames),
                DateTime.Now,
                OpenRead);

            snapshotStore = CreateSnapshotStore(repository);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var blobReferences = snapshot.BlobReferences;

            Assert.Equal(
                expectedNames,
                blobReferences.Select(c => c.Name));
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_LogPositions()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            Assert.Equal(
                snapshotStore.GetSnapshot(logPosition),
                snapshotStore.GetSnapshot(-1));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Ok()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            Assert.True(snapshotStore.CheckSnapshotExists(logPosition));
            Assert.True(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Missing()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            RemoveValues(repository, repository.ListUris());

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
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            CorruptValues(repository, repository.ListUris());

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
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var contentUris = snapshot.BlobReferences
                .SelectMany(blobReference => blobReference.ContentUris);

            RemoveValues(repository, contentUris);

            Assert.False(snapshotStore.CheckSnapshotExists(logPosition));
            Assert.False(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Invalid_Content()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var contentUris = snapshot.BlobReferences
                .SelectMany(blobReference => blobReference.ContentUris);

            CorruptValues(repository, contentUris);

            Assert.True(snapshotStore.CheckSnapshotExists(logPosition));
            Assert.False(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void Restore_Writes_Content_To_Streams()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            var actualBlobName = "";
            using var writeStream = new MemoryStream();

            Stream openWrite(BlobReference blobReference)
            {
                actualBlobName = blobReference.Name;
                return writeStream;
            }

            snapshotStore.RestoreSnapshot(logPosition, "", openWrite);

            Assert.Equal("some content", actualBlobName);
            Assert.Equal(
                ToBytes("some content"),
                writeStream.ToArray());
        }

        [Fact]
        public static void ShowSnapshot_Lists_Content()
        {
            var snapshotStore = CreateSnapshotStore();

            var blobs = CreateBlobs(new[] { "some content" });

            var logPosition = snapshotStore.AppendSnapshot(
                blobs,
                DateTime.Now,
                OpenRead);

            Assert.Equal(
                blobs.Select(b => b.Name),
                snapshotStore.ShowSnapshot(logPosition).Select(c => c.Name));
        }

        [Fact]
        public static void GarbageCollect_Keeps_Used_Uris()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

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
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

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
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            sourceSnapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            sourceRepository.RemoveFromLog(firstLogPosition);

            destinationSnapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some other content" }),
                DateTime.Now,
                OpenRead);

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
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            destinationSnapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some other content" }),
                DateTime.Now,
                OpenRead);

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
                CreateBlobs(new[] { "some content" }),
                DateTime.Now,
                OpenRead);

            sourceSnapshotStore.CopySnapshots(destinationRepository);

            Assert.Equal(
                sourceRepository.ListUris(),
                destinationRepository.ListUris());

            Assert.Equal(
                sourceRepository.ListLogPositions(),
                destinationRepository.ListLogPositions());
        }

        private static Blob[] CreateBlobs(IEnumerable<string> names)
        {
            return names
                .Select(name => new Blob(name, DateTime.Now, DateTime.Now))
                .ToArray();
        }

        private static Stream OpenRead(Blob blob)
        {
            return new MemoryStream(ToBytes(blob.Name));
        }

        private static byte[] ToBytes(string content)
        {
            return Encoding.UTF8.GetBytes(content);
        }

        private static SnapshotStore CreateSnapshotStore(
            IRepository? repository = null,
            bool useCache = false)
        {
            return new SnapshotStore(
                new ContentStore(
                    repository ?? new MemoryRepository(),
                    new FastCdc(),
                    HashAlgorithmName.SHA256),
                new StaticPrompt(),
                useCache);
        }

        private static void CorruptValues(
            IRepository repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
                repository.StoreValue(
                    contentUri,
                    new byte[] { 0xFF, 0xBA, 0xDD, 0xFF });
            }
        }

        private static void RemoveValues(
            IRepository repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
            }
        }
    }
}
