using System.Text.Json;

namespace Chunkyard
{
    /// <summary>
    /// A utility class to convert objects into bytes
    /// </summary>
    public static class DataConvert
    {
        public static byte[] ToBytes(object o)
        {
            return JsonSerializer.SerializeToUtf8Bytes(o);
        }

        public static T ToObject<T>(byte[] value) where T : notnull
        {
            return JsonSerializer.Deserialize<T>(value)!;
        }
    }
}
