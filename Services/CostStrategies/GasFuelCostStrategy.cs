using PowerPlantChallenge.Models;

namespace PowerPlantChallenge.Services.CostStrategies
{
    public class GasFuelCostStrategy : ICostStrategy
    {
        private const double Co2TonsPerMwh = 0.3;

        public double CalculateCostPerMwh(PowerPlant plant, Fuels fuels) =>
            (fuels.Gas / plant.Efficiency) + (Co2TonsPerMwh * fuels.Co2 / plant.Efficiency);

        public double CalculatePmaxAvailable(PowerPlant plant, Fuels fuels) => plant.Pmax;

        public double CalculatePminAvailable(PowerPlant plant) => plant.Pmin;
    }
}
