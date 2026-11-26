using MarketDataApp.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataApp.Controllers;

[ApiController]
[Route("api/seasonality")]
public class SeasonalityController : ControllerBase
{
    private readonly SeasonalityService _seasonalityService;

    public SeasonalityController(SeasonalityService seasonalityService)
    {
        _seasonalityService = seasonalityService;
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetSeasonality(string symbol)
    {
        try
        {
            var data = await _seasonalityService.GetSeasonalityAsync(symbol);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}