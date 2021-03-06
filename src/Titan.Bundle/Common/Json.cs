using System.Text.Json;
using System.Text.Json.Serialization;

namespace Titan.Bundle.Common;

public static class Json
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true,
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static T Deserialize<T>(ReadOnlySpan<byte> json) => JsonSerializer.Deserialize<T>(json, Options);
    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    public static string Serialize<T>(in T value) => JsonSerializer.Serialize(value, Options);
    public static Span<byte> SerializeUtf8<T>(in T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

}