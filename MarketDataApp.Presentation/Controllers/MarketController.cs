using MarketDataApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataApp.Controllers;

[ApiController]
[Route("api/market")]
public class MarketController : ControllerBase
{
    private readonly IMarketService _marketService;

    public MarketController(
        IMarketService marketService)
    {
        _marketService = marketService;
    }

    [HttpGet("candles")]
    public async Task<IActionResult> GetCandles(
        [FromQuery] string symbol,
        [FromQuery] string interval,
        [FromQuery] int limit = 5000)
    {
        try
        {
            var candles =
                await _marketService
                    .GetMarketCandlesAsync(
                        symbol,
                        interval,
                        limit);

            return Ok(candles);
        }
        catch (Exception ex)
        {
            return BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }
}