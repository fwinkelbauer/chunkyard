using Newtonsoft.Json;

namespace Chunkyard.Core
{
    public static class DataConvert
    {
        public static string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public static T DeserializeObject<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }
    }
}
