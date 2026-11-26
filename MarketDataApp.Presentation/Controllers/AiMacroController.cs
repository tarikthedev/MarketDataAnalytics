using MarketDataApp.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiMacroController : ControllerBase
{
    private readonly AiMacroService _aiService;

    public AiMacroController(AiMacroService aiService)
    {
        _aiService = aiService;
    }

    [HttpGet("analyze")]
    public async Task<IActionResult> GetAiAnalysis([FromQuery] string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return BadRequest("Symbol gerekli.");

        try
        {
            var report = await _aiService.GenerateTradeHypothesisAsync(symbol.ToUpper());
            return Ok(report);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}