using System.Collections.Generic;

namespace Chunkyard
{
    public interface IContentRefLog<T> where T : IContentRef
    {
        int Store(T contentRef, string logName, int currentLogPosition);

        T Retrieve(string logName, int logPosition);

        bool TryFetchLogPosition(string logName, out int currentLogPosition);

        IEnumerable<int> List(string logName);
    }
}
