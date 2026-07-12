using PowerPlantChallenge.Models;

namespace PowerPlantChallenge.Services.CostStrategies
{
    public class KerosineFuelCostStrategy : ICostStrategy
    {
        public double CalculateCostPerMwh(PowerPlant plant, Fuels fuels) =>
            fuels.Kerosine / plant.Efficiency;

        public double CalculatePmaxAvailable(PowerPlant plant, Fuels fuels) => plant.Pmax;

        public double CalculatePminAvailable(PowerPlant plant) => plant.Pmin;
    }
}
