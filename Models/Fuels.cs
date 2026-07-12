using System.Text.Json.Serialization;

namespace PowerPlantChallenge.Models
{
    public class Fuels
    {
        [JsonPropertyName("gas(euro/MWh)")]
        public double Gas { get; set; }

        [JsonPropertyName("kerosine(euro/MWh)")]
        public double Kerosine { get; set; }

        [JsonPropertyName("co2(euro/ton)")]
        public double Co2 { get; set; }

        // Not a fuel price like the others above - this is the current wind
        // availability (%), shared across every windturbine plant. Wind turbines
        // consume no fuel and are always priced at zero; this value only scales
        // their available output (Pmax * wind% / 100).
        [JsonPropertyName("wind(%)")]
        public double Wind { get; set; }
    }
}
