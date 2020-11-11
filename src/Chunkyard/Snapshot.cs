﻿using System;
using System.Collections.Immutable;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A snapshot contains a list of references which describe the state of
    /// several files at a specific point in time.
    /// </summary>
    public class Snapshot
    {
        public const int SchemaVersion = 1;

        public Snapshot(
            int version,
            DateTime creationTime,
            IImmutableList<ContentReference> contentReferences)
        {
            Version = version;
            CreationTime = creationTime;
            ContentReferences = contentReferences;

            if (Version != SchemaVersion)
            {
                throw new ChunkyardException(
                    $"Unsupported snapshot schema v{Version}");
            }
        }

        public int Version { get; }

        public DateTime CreationTime { get; }

        public IImmutableList<ContentReference> ContentReferences { get; }

        public override bool Equals(object? obj)
        {
            return obj is Snapshot snapshot
                && Version == snapshot.Version
                && CreationTime == snapshot.CreationTime
                && ContentReferences.SequenceEqual(snapshot.ContentReferences);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Version, CreationTime, ContentReferences);
        }
    }
}
