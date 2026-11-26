using MarketDataApp.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataApp.Controllers;

[ApiController]
[Route("api/squeeze")]
public class SqueezeController : ControllerBase
{
    private readonly SqueezeService _squeezeService;

    public SqueezeController(SqueezeService squeezeService)
    {
        _squeezeService = squeezeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSqueezeScanner()
    {
        try
        {
            var data = await _squeezeService.GetAllSqueezeAnalysisAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}