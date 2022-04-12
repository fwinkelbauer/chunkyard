using System.Text.Json.Serialization;

namespace Chunkyard.Core;

[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(SnapshotReference))]
internal partial class JsonContext : JsonSerializerContext
{
}
