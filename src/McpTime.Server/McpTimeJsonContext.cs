using System.Text.Json.Serialization;
using McpTime.Core;

namespace McpTime.Server;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(TimeResult))]
internal sealed partial class McpTimeJsonContext : JsonSerializerContext;
