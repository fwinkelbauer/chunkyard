﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A reference which describes how to retrieve encrypted and chunked data
    /// from a <see cref="ContentStore"/>.
    /// </summary>
    public class ContentReference
    {
        public ContentReference(
            string name,
            byte[] nonce,
            IEnumerable<ChunkReference> chunks,
            ContentType type)
        {
            Name = name;
            Nonce = nonce;
            Chunks = chunks.ToArray();
            Type = type;
        }

        public string Name { get; }

        public byte[] Nonce { get; }

        public IEnumerable<ChunkReference> Chunks { get; }

        public ContentType Type { get; }

        public override bool Equals(object? obj)
        {
            return obj is ContentReference reference
                && Name == reference.Name
                && Nonce.SequenceEqual(reference.Nonce)
                && Chunks.SequenceEqual(reference.Chunks)
                && Type == reference.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Nonce, Chunks, Type);
        }
    }
}
