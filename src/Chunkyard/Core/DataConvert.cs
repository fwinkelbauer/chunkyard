namespace Chunkyard.Core;

/// <summary>
/// A utility class to convert objects and text into bytes.
/// </summary>
public static class DataConvert
{
    public static byte[] ObjectToBytes(object o)
    {
        return JsonSerializer.SerializeToUtf8Bytes(o);
    }

    public static T BytesToObject<T>(byte[] json) where T : notnull
    {
        var t = JsonSerializer.Deserialize<T>(json);

        if (t == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        return t;
    }

    public static T BytesToVersionedObject<T>(
        byte[] json,
        int supportedSchemaVersion)
        where T : notnull, IVersioned
    {
        var versioned = BytesToObject<Versioned>(json);

        if (versioned.SchemaVersion != supportedSchemaVersion)
        {
            throw new NotSupportedException(
                $"{typeof(T).Name} version {versioned.SchemaVersion} is not supported");
        }

        return BytesToObject<T>(json);
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
