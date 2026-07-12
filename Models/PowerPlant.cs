using System.Text.Json.Serialization;
using PowerPlantChallenge.Serialization;

namespace PowerPlantChallenge.Models
{
    public class PowerPlant
    {
        public string Name { get; set; } = string.Empty;

        [JsonConverter(typeof(PlantTypeJsonConverter))]
        public PowerPlantType Type { get; set; }

        public double Efficiency { get; set; }
        public double Pmax { get; set; }
        public double Pmin { get; set; }
    }
}
