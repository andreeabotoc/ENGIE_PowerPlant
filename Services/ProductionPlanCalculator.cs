using PowerPlantChallenge.ExceptionHandling;
using PowerPlantChallenge.Models;
using PowerPlantChallenge.Services.CostStrategies;

namespace PowerPlantChallenge.Services;

public class ProductionPlanCalculator : IProductionPlanCalculator
{
    private static readonly Dictionary<PowerPlantType, ICostStrategy> Strategies = new()
    {
        [PowerPlantType.GasFired] = new GasFuelCostStrategy(),
        [PowerPlantType.Turbojet] = new KerosineFuelCostStrategy(),
        [PowerPlantType.WindTurbine] = new WindFuelCostStrategy(),
    };
    private static ICostStrategy GetCostStrategy(PowerPlantType type) =>
       Strategies.TryGetValue(type, out var strategy)
           ? strategy
           : throw new ValidationException($"Unknown powerplant type: {type}");


    public List<PowerPlantResult> Calculate(ProductionPlanRequest request)
    {
        #region << Validate request received >>
        Validate(request);
        #endregion << Validate request received >>

        #region << Calculate cost per MWh for each plant>>
        var plants = request.Powerplants
            .Select(p =>
            {
            var strategy = GetCostStrategy(p.Type);
                return new PlantCalculation
                {
                    Plant = p,
                    CostPerMwh = strategy.CalculateCostPerMwh(p, request.Fuels),
                    PmaxAvailable = strategy.CalculatePmaxAvailable(p, request.Fuels),
                    PminAvailable = strategy.CalculatePminAvailable(p)
                };
            })
            .ToList();
        #endregion << Calculate cost per MWh for each plant>>

        #region << Sorting depending on the merit order - cheapest first >>
        var meritOrder = plants.OrderBy(p => p.CostPerMwh).ToList();
        #endregion << Sorting depending on the merit order - cheapest first >>

        #region << Provide load(power loading) depending on the merit order >>
        AllocateLoad(meritOrder, request.Load);
        #endregion << Provide load depending on the merit order >>

        #region << Return result - each power plant with it's loading >>
        return plants
            .Select(p => new PowerPlantResult
            {
                Name = p.Plant.Name,
                Power = Math.Round(p.Output, 1)
            })
            .ToList();
        #endregion << Return result - each power plant with it's loading >>
    }

    /// <summary>
    /// Validates the received production plan request
    /// </summary>
    /// <param name="request">Production plan request received</param>
    /// <exception cref="ValidationException">Validation exception depending on the conditions:
    ///1.we expect to have at least one powerplant,
    ///2.the load has to be greater than zero
    ///3.the fuel prices have to be provided
    ///4.every powerplant's Pmin must not exceed its Pmax
    ///5.no powerplant's Pmin may be negative (which, combined with 4, also rules out a negative Pmax)
    ///6.every powerplant whose type needs a fuel efficiency must have a positive one
    /// </exception>
    private static void Validate(ProductionPlanRequest request)
    {
        if (request.Powerplants == null || request.Powerplants.Count == 0)
        {
            throw new ValidationException("At least one powerplant must be provided.");
        }

        if (request.Load <= 0)
        {
            throw new ValidationException("Load must be greater than zero.");
        }

        if (request.Fuels == null)
        {
            throw new ValidationException("Fuel prices must be provided.");
        }

        foreach (var plant in request.Powerplants)
        {
            if (plant.Pmin > plant.Pmax)
            {
                throw new ValidationException(
                    $"Plant '{plant.Name}' has Pmin ({plant.Pmin}) greater than Pmax ({plant.Pmax}).");
            }

            if (plant.Pmin < 0)
            {
                throw new ValidationException($"Plant '{plant.Name}' has a negative Pmin ({plant.Pmin}).");
            }

            if (GetCostStrategy(plant.Type).RequiresPositiveEfficiency && plant.Efficiency <= 0)
            {
                throw new ValidationException(
                    $"Plant '{plant.Name}' has a non-positive efficiency ({plant.Efficiency}).");
            }
        }
    }

    
    /// <summary>
    /// Distributes the requested load across the plants
    /// in <paramref name="powerPlantsByMeritOrder"/> (cheapest cost-per-MWh first), setting each
    /// plant's <see cref="PlantCalculation.Output"/>, filling each plant up to its
    /// max output before moving on to the next one.
    ///
    /// Depending on the type of the PowerPlant,
    /// some have a minimum stable output(Pmin) below which they can't operate,
    /// and a maximum generated output (Pmax).
    /// So when the load remaining for a plant falls between zero and that plant's Pmin,
    /// we can't just give it "whatever's left": we either commit it at its Pmin (and
    /// get the excess back from the more expensive plants already allocated, since
    /// they're allowed to give some of it up) or, if no such trade-off exists, the load
    /// is not feasible with this set of plants.
    /// </summary>
    /// <param name="powerPlantsByMeritOrder">Plants with their cost/output figures, cheapest first.</param>
    /// <param name="load">Total load (MW) that must be covered exactly.</param>
    /// <exception cref="ValidationException">
    /// Thrown when the requested load cannot be met at all (not enough total capacity),
    /// or when a plant's Pmin can't be absorbed without violating another plant's Pmin.
    /// </exception>
    private static void AllocateLoad(List<PlantCalculation> powerPlantsByMeritOrder, double load)
    {
        double remainingLoad = load;
        var alreadyAllocated = new List<PlantCalculation>();

        foreach (var plant in powerPlantsByMeritOrder)
        {
            if (remainingLoad <= 0)
            {
                break;
            }

            remainingLoad = AllocateToPlant(plant, alreadyAllocated, remainingLoad);
            alreadyAllocated.Add(plant);
        }

        // 0.01 instead of 0: chained double arithmetic/rounding can leave a
        // microscopic leftover (e.g. 1e-13) even when the load is genuinely fully
        // covered. 0.01 is an order of magnitude below the 0.1 MW precision the
        // API actually reports, so it absorbs that floating-point noise without
        // masking a real shortfall.
        if (remainingLoad > 0.01)
        {
            throw new ValidationException(
                "Unable to satisfy the requested load with the available powerplants.");
        }
    }

    /// <summary>
    /// Allocates as much of <paramref name="remainingLoad"/> as possible to
    /// <paramref name="plant"/>, and returns the load still left afterwards.
    /// </summary>
    private static double AllocateToPlant(PlantCalculation plant, List<PlantCalculation> alreadyAllocated, double remainingLoad)
    {
        if (remainingLoad >= plant.PmaxAvailable)
        {
            // Enough load left to run this plant flat out: take all of it and move on.
            plant.Output = plant.PmaxAvailable;
            return remainingLoad - plant.PmaxAvailable;
        }

        if (remainingLoad >= plant.PminAvailable)
        {
            // What's left fits within this plant's operating range [Pmin, Pmax]:
            // it absorbs the remainder exactly and the load is fully covered.
            plant.Output = Math.Round(remainingLoad, 1);
            return 0;
        }

        // remainingLoad < Pmin: this plant must run at its minimum. Reduce
        // cheaper, already-allocated plants to free up the room it needs.
        ReduceEarlierPlants(alreadyAllocated, shortfall: plant.PminAvailable - remainingLoad, plant.Plant.Name);
        plant.Output = plant.PminAvailable;
        return 0;
    }

    /// <summary>
    /// Frees up <paramref name="shortfall"/> MW by reducing the output of
    /// already-allocated plants, starting with the most expensive of them (the
    /// end of the list, since it follows merit order). Throws if reducing every
    /// eligible plant down to its own minimum still isn't enough.
    /// </summary>
    private static void ReduceEarlierPlants(List<PlantCalculation> alreadyAllocated, double shortfall, string plantName)
    {
        foreach (var earlier in Enumerable.Reverse(alreadyAllocated))
        {
            if (shortfall <= 0)
            {
                break;
            }

            var roomToReduce = earlier.Output - earlier.PminAvailable;
            if (roomToReduce <= 0)
            {
                continue;
            }

            var reduction = Math.Min(roomToReduce, shortfall);
            earlier.Output = Math.Round(earlier.Output - reduction, 1);
            shortfall -= reduction;
        }

        // Same floating-point tolerance as AllocateLoad's feasibility check below:
        // 0.01 absorbs rounding noise without masking a genuine shortfall.
        if (shortfall > 0.01)
        {
            throw new ValidationException(
                $"Unable to find a feasible production plan: cannot satisfy Pmin constraint for {plantName} without violating another plant's Pmin.");
        }
    }

    // These numbers depend on this request's fuel prices and load, not just on
    // the plant - the same PowerPlant can get a different CostPerMwh/PmaxAvailable
    // on a different request. That's why they live here and not on PowerPlant itself.
    private class PlantCalculation
    {
        public required PowerPlant Plant { get; init; }
        public double CostPerMwh { get; init; }
        public double PmaxAvailable { get; init; }
        public double PminAvailable { get; init; }
        public double Output { get; set; }
    }
}