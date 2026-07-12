using PowerPlantChallenge.Models;

namespace PowerPlantChallenge.Services;

public interface IProductionPlanCalculator
{
    List<PowerPlantResult> Calculate(ProductionPlanRequest request);
}
