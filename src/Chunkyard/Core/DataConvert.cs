using System.Text;
using System.Text.Json;

namespace Chunkyard.Core
{
    /// <summary>
    /// A utility class to convert objects and text into bytes
    /// </summary>
    public static class DataConvert
    {
        public static byte[] ObjectToBytes(object o)
        {
            return JsonSerializer.SerializeToUtf8Bytes(o);
        }

        public static T BytesToObject<T>(byte[] value) where T : notnull
        {
            return JsonSerializer.Deserialize<T>(value)!;
        }

        public static byte[] TextToBytes(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        public static string BytesToText(byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }
    }
}
