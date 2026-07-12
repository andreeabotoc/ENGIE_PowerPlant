using PowerPlantChallenge.Models;

namespace PowerPlantChallenge.Services.CostStrategies
{
    public class WindFuelCostStrategy : ICostStrategy
    {
        public double CalculateCostPerMwh(PowerPlant plant, Fuels fuels) => 0;

        public double CalculatePmaxAvailable(PowerPlant plant, Fuels fuels) =>
            Math.Round(plant.Pmax * fuels.Wind / 100.0, 1);

        public double CalculatePminAvailable(PowerPlant plant) => 0;

        // Wind has no fuel to convert, so efficiency is irrelevant here.
        public bool RequiresPositiveEfficiency => false;
    }
}
