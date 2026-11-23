using System.Text.Json;
using System.Text.Json.Serialization;

namespace CredentialAuthority.Api.Serialization;

internal sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("A date and time with an explicit time zone is required.");
        }

        var text = reader.GetString()!;
        var hasOffset = text.Length >= 6 && text[^6] is '+' or '-' && text[^3] == ':';
        if ((!text.EndsWith('Z') && !hasOffset) || !reader.TryGetDateTimeOffset(out var value))
        {
            throw new JsonException("Use an ISO 8601 date and time with Z or an explicit offset.");
        }

        return value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.UtcDateTime);
    }
}
