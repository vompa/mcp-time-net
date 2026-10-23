using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpTime.Server;

internal static class ToolSchemas
{
    public static JsonElement GetCurrentTimeInput(string localTimeZone) => ToElement(new JsonObject
    {
        ["title"] = "GetCurrentTimeInput",
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["timezone"] = Text($"IANA time zone name such as 'America/New_York' or 'Europe/London'. Use '{localTimeZone}' (the server's local time zone) if the user does not name one."),
        },
        ["required"] = Names("timezone"),
    });

    public static JsonElement ConvertTimeInput(string localTimeZone) => ToElement(new JsonObject
    {
        ["title"] = "ConvertTimeInput",
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["source_timezone"] = Text($"IANA time zone the given time is in, such as 'Europe/Vienna'. Use '{localTimeZone}' (the server's local time zone) if the user does not name a source time zone."),
            ["time"] = Text("Time to convert in 24-hour HH:MM format, such as '14:30'."),
            ["target_timezone"] = Text($"IANA time zone to convert to, such as 'Asia/Tokyo'. Use '{localTimeZone}' (the server's local time zone) if the user does not name a target time zone."),
            ["date"] = Text("Optional date of the time to convert in YYYY-MM-DD format, such as '2024-03-10'. Defaults to today in the source time zone."),
        },
        ["required"] = Names("source_timezone", "time", "target_timezone"),
    });

    public static JsonElement TimeResultOutput() => ToElement(TimeResultSchema(description: null));

    public static JsonElement TimeConversionResultOutput() => ToElement(new JsonObject
    {
        ["title"] = "TimeConversionResult",
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["source"] = TimeResultSchema("The given time in the source time zone."),
            ["target"] = TimeResultSchema("The same moment in the target time zone."),
            ["time_difference"] = Text("UTC offset of the target minus the source time zone in hours, such as +5.75h or -4.0h."),
        },
        ["required"] = Names("source", "target", "time_difference"),
    });

    private static JsonObject TimeResultSchema(string? description)
    {
        var schema = new JsonObject { ["title"] = "TimeResult", ["type"] = "object" };
        if (description is not null)
        {
            schema["description"] = description;
        }

        schema["properties"] = new JsonObject
        {
            ["timezone"] = Text("IANA time zone name exactly as requested."),
            ["datetime"] = Text("ISO 8601 date and time with UTC offset, such as 2024-01-01T13:00:00+01:00."),
            ["day_of_week"] = Text("English weekday name, such as Monday."),
            ["is_dst"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "True when daylight saving time is in effect at that moment.",
            },
        };
        schema["required"] = Names("timezone", "datetime", "day_of_week", "is_dst");
        return schema;
    }

    private static JsonObject Text(string description)
        => new() { ["type"] = "string", ["description"] = description };

    private static JsonArray Names(params string[] names)
        => new([.. names.Select(name => (JsonNode)JsonValue.Create(name)!)]);

    private static JsonElement ToElement(JsonObject schema)
    {
        using var document = JsonDocument.Parse(schema.ToJsonString());
        return document.RootElement.Clone();
    }
}
