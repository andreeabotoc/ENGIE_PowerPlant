using System.Text.Json;
using System.Text.Json.Serialization;
using PowerPlantChallenge.Models;

namespace PowerPlantChallenge.Serialization;

public class PlantTypeJsonConverter : JsonConverter<PowerPlantType>
{
    private static readonly Dictionary<string, PowerPlantType> FromWire = new()
    {
        ["gasfired"] = PowerPlantType.GasFired,
        ["turbojet"] = PowerPlantType.Turbojet,
        ["windturbine"] = PowerPlantType.WindTurbine,
    };

    private static readonly Dictionary<PowerPlantType, string> ToWire =
        FromWire.ToDictionary(kv => kv.Value, kv => kv.Key);

    public override PowerPlantType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? string.Empty;
        if (!FromWire.TryGetValue(value, out var plantType))
        {
            throw new JsonException($"Unknown powerplant type: {value}");
        }

        return plantType;
    }

    public override void Write(Utf8JsonWriter writer, PowerPlantType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ToWire[value]);
    }
}
