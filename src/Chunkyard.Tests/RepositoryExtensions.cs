using System;
using System.Collections.Generic;
using Chunkyard.Core;

namespace Chunkyard.Tests
{
    internal static class RepositoryExtensions
    {
        public static void CorruptValues<T>(
            this IRepository<T> repository,
            IEnumerable<T> keys)
        {
            foreach (var key in keys)
            {
                repository.RemoveValue(key);
                repository.StoreValue(
                    key,
                    new byte[] { 0xFF, 0xBA, 0xDD, 0xFF });
            }
        }

        public static void RemoveValues<T>(
            this IRepository<T> repository,
            IEnumerable<T> keys)
        {
            foreach (var key in keys)
            {
                repository.RemoveValue(key);
            }
        }
    }
}
