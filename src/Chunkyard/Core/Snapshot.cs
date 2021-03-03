﻿using System;
using System.Collections.Immutable;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A snapshot contains a list of references which describe the state of
    /// several files at a specific point in time.
    /// </summary>
    public class Snapshot
    {
        public Snapshot(
            DateTime creationTime,
            IImmutableList<ContentReference> contentReferences)
        {
            CreationTime = creationTime;
            ContentReferences = contentReferences;
        }

        public DateTime CreationTime { get; }

        public IImmutableList<ContentReference> ContentReferences { get; }

        public override bool Equals(object? obj)
        {
            return obj is Snapshot other
                && CreationTime == other.CreationTime
                && ContentReferences.SequenceEqual(other.ContentReferences);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CreationTime, ContentReferences);
        }
    }
}