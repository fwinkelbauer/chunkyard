﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// An implementation of <see cref="IContentReference"/> to store binary
    /// data in an <see cref="ContentStore"/>.
    /// </summary>
    public class BlobReference : IContentReference
    {
        public BlobReference(
            string name,
            DateTime lastWriteTimeUtc,
            byte[] nonce,
            IReadOnlyCollection<Uri> contentUris)
        {
            Name = name;
            LastWriteTimeUtc = lastWriteTimeUtc;
            Nonce = nonce;
            ContentUris = contentUris;
        }

        public string Name { get; }

        public DateTime LastWriteTimeUtc { get; }

        public byte[] Nonce { get; }

        public IReadOnlyCollection<Uri> ContentUris { get; }

        public Blob ToBlob()
        {
            return new Blob(Name, LastWriteTimeUtc);
        }

        public override bool Equals(object? obj)
        {
            return obj is BlobReference other
                && Name == other.Name
                && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc)
                && Nonce.SequenceEqual(other.Nonce)
                && ContentUris.SequenceEqual(other.ContentUris);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name,
                LastWriteTimeUtc,
                Nonce,
                ContentUris);
        }
    }
}
