using System.Text;
using Newtonsoft.Json;

namespace Chunkyard
{
    /// <summary>
    /// A utility class to convert objects into bytes
    /// </summary>
    public static class DataConvert
    {
        public static byte[] ToBytes(object o)
        {
            return Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(o));
        }

        public static T ToObject<T>(byte[] value) where T : notnull
        {
            return JsonConvert.DeserializeObject<T>(
                Encoding.UTF8.GetString(value));
        }
    }
}
