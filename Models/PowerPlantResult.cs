using PowerPlantChallenge.Serialization;
using System.Text.Json.Serialization;

namespace PowerPlantChallenge.Models
{
    public class PowerPlantResult
    {
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("p")]
        [JsonConverter(typeof(OneDecimalJsonConverter))]
        public double Power { get; set; }
    }
}
