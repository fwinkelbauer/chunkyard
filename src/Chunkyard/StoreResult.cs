using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// Contains information about stored data in a <see cref="ContentStore"/>.
    /// </summary>
    public class StoreResult
    {
        public StoreResult(
            ContentReference contentReference,
            bool newContent)
        {
            ContentReference = contentReference;
            NewContent = newContent;
        }

        public ContentReference ContentReference { get; }

        public bool NewContent { get; }

        public override bool Equals(object? obj)
        {
            return obj is StoreResult result
                && ContentReference.Equals(result.ContentReference)
                && NewContent == result.NewContent;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContentReference, NewContent);
        }
    }
}
