using Microsoft.AspNetCore.Mvc;
using PowerPlantChallenge.Models;
using PowerPlantChallenge.Services;

namespace PowerPlantChallenge.Controllers;

[ApiController]
[Route("productionplan")]
public class ProductionPlanController : ControllerBase
{
    private readonly ILogger<ProductionPlanController> _logger;
    private readonly IProductionPlanCalculator _calculator;

    public ProductionPlanController(
        ILogger<ProductionPlanController> logger,
        IProductionPlanCalculator calculator)
    {
        _logger = logger;
        _calculator = calculator;
    }

    [HttpPost]
    public IActionResult Post([FromBody] ProductionPlanRequest request)
    {
        _logger.LogInformation("Received production plan request for load {Load}", request.Load);

        var result = _calculator.Calculate(request);

        _logger.LogInformation("Production plan calculated successfully for load {Load}", request.Load);

        return Ok(result);
    }
}