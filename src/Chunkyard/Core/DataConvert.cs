using System;
using System.Text.Json;

namespace Chunkyard.Core
{
    /// <summary>
    /// A utility class to convert objects into bytes
    /// </summary>
    internal static class DataConvert
    {
        public static byte[] ToBytes(object o)
        {
            return JsonSerializer.SerializeToUtf8Bytes(o);
        }

        public static T ToObject<T>(byte[] value) where T : notnull
        {
            try
            {
                return JsonSerializer.Deserialize<T>(value)!;
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Could not deserialize data to {typeof(T).Name}",
                    e);
            }
        }
    }
}
