using MarketDataApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataApp.Controllers;

[ApiController]
[Route("api/cot")]
public class CotController : ControllerBase
{
    private readonly ICotService _cotService;

    public CotController(ICotService cotService)
    {
        _cotService = cotService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetCotOverview([FromQuery] int weeksAgo = 0)
    {
        var result = await _cotService.GetCotOverviewAsync(weeksAgo);
        return Ok(result);
    }

    [HttpGet("history/{currency}")]
    public async Task<IActionResult> GetCotHistory(string currency)
    {
        var result = await _cotService.GetCotHistoryAsync(currency);
        return Ok(result);
    }

    [HttpGet("backtest")]
    public async Task<IActionResult> RunBacktest([FromQuery] int weeks = 10)
    {
        var result = await _cotService.RunBacktestAsync(weeks);
        return Ok(result);
    }
}