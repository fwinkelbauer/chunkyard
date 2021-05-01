using System;
using System.Collections.Generic;
using Chunkyard.Core;

namespace Chunkyard.Tests
{
    internal static class RepositoryExtensions
    {
        public static void CorruptValues(
            this IRepository<Uri> repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
                repository.StoreValue(
                    contentUri,
                    new byte[] { 0xFF, 0xBA, 0xDD, 0xFF });
            }
        }

        public static void RemoveValues(
            this IRepository<Uri> repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
            }
        }
    }
}
