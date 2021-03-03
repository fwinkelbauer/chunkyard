using System.Collections.Concurrent;

namespace Chunkyard.Core
{
    /// <summary>
    /// A class which provides named objects which can be used as locks. Based
    /// on this blog post:
    ///
    /// https://www.tabsoverspaces.com/233703-named-locks-using-monitor-in-net-implementation
    /// </summary>
    internal class NamedMonitor
    {
        private readonly ConcurrentDictionary<string, object> _dictionary;

        public NamedMonitor()
        {
            _dictionary = new ConcurrentDictionary<string, object>();
        }

        public object this[string name]
            => _dictionary.GetOrAdd(name, _ => new object());
    }
}
