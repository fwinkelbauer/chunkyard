using System;

namespace Chunkyard.Tests
{
    public class UnstoredRepository : DecoratorRepository
    {
        public UnstoredRepository()
            : base(new MemoryRepository())
        {
        }

        public override bool StoreValue(Uri contentUri, byte[] value)
        {
            // Let's pretend that we lose stored values by not storing them
            return true;
        }
    }
}
