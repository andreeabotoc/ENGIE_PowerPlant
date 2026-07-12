namespace PowerPlantChallenge.Models
{
    public class ProductionPlanRequest
    {
        public double Load { get; set; }
        public Fuels Fuels { get; set; } = new();
        public List<PowerPlant> Powerplants { get; set; } = new();
    }
}
