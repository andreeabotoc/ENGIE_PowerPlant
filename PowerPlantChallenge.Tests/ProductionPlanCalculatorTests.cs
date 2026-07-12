using PowerPlantChallenge.ExceptionHandling;
using PowerPlantChallenge.Models;
using PowerPlantChallenge.Services;

namespace PowerPlantChallenge.Tests;

public class ProductionPlanCalculatorTests
{
    private readonly ProductionPlanCalculator _calculator = new();

    private static List<PowerPlant> DefaultPlants() => new()
    {
        new PowerPlant { Name = "gasfiredbig1", Type = PowerPlantType.GasFired, Efficiency = 0.53, Pmin = 100, Pmax = 460 },
        new PowerPlant { Name = "gasfiredbig2", Type = PowerPlantType.GasFired, Efficiency = 0.53, Pmin = 100, Pmax = 460 },
        new PowerPlant { Name = "gasfiredsomewhatsmaller", Type = PowerPlantType.GasFired, Efficiency = 0.37, Pmin = 40, Pmax = 210 },
        new PowerPlant { Name = "tj1", Type = PowerPlantType.Turbojet, Efficiency = 0.3, Pmin = 0, Pmax = 16 },
        new PowerPlant { Name = "windpark1", Type = PowerPlantType.WindTurbine, Efficiency = 1, Pmin = 0, Pmax = 150 },
        new PowerPlant { Name = "windpark2", Type = PowerPlantType.WindTurbine, Efficiency = 1, Pmin = 0, Pmax = 36 },
    };

    private static Fuels DefaultFuels(double wind) => new()
    {
        Gas = 13.4,
        Kerosine = 50.8,
        Co2 = 20,
        Wind = wind
    };

    [Fact]
    public void Calculate_Payload3_MatchesExpectedResponse()
    {
        // Arrange — matches example_payloads/payload3.json and response3.json
        var request = new ProductionPlanRequest
        {
            Load = 910,
            Fuels = DefaultFuels(wind: 60),
            Powerplants = DefaultPlants()
        };

        // Act
        var result = _calculator.Calculate(request);

        // Assert
        Assert.Equal(90.0, Get(result, "windpark1"));
        Assert.Equal(21.6, Get(result, "windpark2"));
        Assert.Equal(460.0, Get(result, "gasfiredbig1"));
        Assert.Equal(338.4, Get(result, "gasfiredbig2"));
        Assert.Equal(0.0, Get(result, "gasfiredsomewhatsmaller"));
        Assert.Equal(0.0, Get(result, "tj1"));
    }

    [Fact]
    public void Calculate_PminConflict_ReducesMostExpensiveAllocatedPlant()
    {
        // Arrange — load = 940, wind off. Forces gasfiredsomewhatsmaller's Pmin
        // conflict: remaining after the two big gas plants is 20, below its Pmin of 40.
        var request = new ProductionPlanRequest
        {
            Load = 940,
            Fuels = DefaultFuels(wind: 0),
            Powerplants = DefaultPlants()
        };

        // Act
        var result = _calculator.Calculate(request);

        // Assert
        Assert.Equal(460.0, Get(result, "gasfiredbig1"));
        Assert.Equal(440.0, Get(result, "gasfiredbig2")); // reduced by 20 to make room
        Assert.Equal(40.0, Get(result, "gasfiredsomewhatsmaller")); // switched on at Pmin
        Assert.Equal(0.0, Get(result, "tj1"));

        // Total must always equal the requested load
        var total = result.Sum(r => r.Power);
        Assert.Equal(940.0, total, precision: 1);
    }

    [Fact]
    public void Calculate_UnresolvableMinConflict_ThrowsValidationException()
    {
        // Arrange: plantA (cheapest, allocated first) has Pmin == Pmax, so it has
        // no headroom to give back. plantB then needs its Pmin (30) but only 10 MW
        // remain; the 20 MW excess can't be clawed back from plantA and there's no
        // other allocated plant to absorb it, so no feasible plan exists.
        var request = new ProductionPlanRequest
        {
            Load = 60,
            Fuels = new Fuels { Gas = 10, Kerosine = 50, Co2 = 0, Wind = 0 },
            Powerplants = new List<PowerPlant>
            {
                new PowerPlant { Name = "plantA", Type = PowerPlantType.GasFired, Efficiency = 1, Pmin = 50, Pmax = 50 },
                new PowerPlant { Name = "plantB", Type = PowerPlantType.Turbojet, Efficiency = 1, Pmin = 30, Pmax = 100 },
            }
        };

        var ex = Assert.Throws<ValidationException>(() => _calculator.Calculate(request));
        Assert.Contains("cannot satisfy Pmin constraint", ex.Message);
    }

    [Fact]
    public void Calculate_LoadExceedsTotalCapacity_ThrowsValidationException()
    {
        // Arrange: with wind off, total available capacity is 1146 MW
        // (460 + 460 + 210 + 16); requesting more than that is infeasible.
        var request = new ProductionPlanRequest
        {
            Load = 1200,
            Fuels = DefaultFuels(wind: 0),
            Powerplants = DefaultPlants()
        };

        var ex = Assert.Throws<ValidationException>(() => _calculator.Calculate(request));
        Assert.Equal("Unable to satisfy the requested load with the available powerplants.", ex.Message);
    }

    private static double Get(List<PowerPlantResult> results, string name) =>
        results.First(r => r.Name == name).Power;
}
