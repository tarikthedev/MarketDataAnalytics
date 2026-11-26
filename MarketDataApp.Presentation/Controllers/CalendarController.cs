using Microsoft.AspNetCore.Mvc;
using MarketDataApp.Business.Services;

namespace MarketDataApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly AiMacroService _aiMacroService;

        public CalendarController(AiMacroService aiMacroService)
        {
            _aiMacroService = aiMacroService;
        }

        // AI Ajanının kullandığı (Sadece seçilen paritenin haberleri)
        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetEvents(string symbol)
        {
            var events = await _aiMacroService.GetUpcomingHighImpactEventsAsync(symbol);
            return Ok(events);
        }

        // TAKVİM SAYFASININ KULLANDIĞI (Bu haftanın tüm yüksek etkili haberleri)
        [HttpGet("all")]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _aiMacroService.GetAllHighImpactEventsAsync();
            return Ok(events);
        }
    }
}