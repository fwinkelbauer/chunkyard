using System.Text;
using Newtonsoft.Json;

namespace Chunkyard
{
    public static class SerializationExtensions
    {
        public static byte[] ToBytes(this object o)
        {
            return Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(o));
        }

        public static T ToObject<T>(this byte[] value)
        {
            return JsonConvert.DeserializeObject<T>(
                Encoding.UTF8.GetString(value));
        }
    }
}
