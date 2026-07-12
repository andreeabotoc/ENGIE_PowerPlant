using PowerPlantChallenge.Models;

namespace PowerPlantChallenge.Services.CostStrategies
{
    public interface ICostStrategy
    {
        double CalculateCostPerMwh(PowerPlant plant, Fuels fuels);
        double CalculatePmaxAvailable(PowerPlant plant, Fuels fuels);
        double CalculatePminAvailable(PowerPlant plant);

        /// <summary>
        /// Whether this plant type's cost calculation actually uses Efficiency.
        /// True for fuel-based types (gas, turbojet, and any future fuel-based
        /// type); wind overrides this to false since there's no fuel to convert.
        /// </summary>
        bool RequiresPositiveEfficiency => true;
    }
}
