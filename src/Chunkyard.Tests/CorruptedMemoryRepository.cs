using System;

namespace Chunkyard.Tests
{
    internal class CorruptedRepository : DecoratorRepository
    {
        public CorruptedRepository()
            : base(new MemoryRepository())
        {
        }

        public override bool StoreValue(Uri contentUri, byte[] value)
        {
            return base.StoreValue(
                contentUri,
                new byte[] { 0xFF, 0xBA, 0xD0, 0xFF });
        }
    }
}
