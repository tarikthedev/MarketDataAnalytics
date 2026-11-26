using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MarketDataApp.Core.DTOs;
using MarketDataApp.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace MarketDataApp.Business.Services;

public class AiMacroService
{
    private readonly HttpClient _httpClient;
    private readonly SqueezeService _squeezeService;
    private readonly IMarketService _marketService;
    private readonly ICotService _cotService;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public AiMacroService(
        HttpClient httpClient,
        SqueezeService squeezeService,
        IMarketService marketService,
        ICotService cotService,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _squeezeService = squeezeService;
        _marketService = marketService;
        _cotService = cotService;
        _cache = cache;
        _configuration = configuration;
    }

    // 1. FOREX FACTORY EKONOMİK TAKVİM OKUYUCUSU
    public async Task<List<CalendarEvent>> GetUpcomingHighImpactEventsAsync(string symbol)
    {
        return await _cache.GetOrCreateAsync($"CALENDAR_{symbol}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);

            var currencies = new List<string> { symbol.Substring(0, 3), symbol.Substring(3, 3) };
            var events = new List<CalendarEvent>();

            try
            {
                var response = await _httpClient.GetStringAsync("https://nfs.faireconomy.media/ff_calendar_thisweek.xml");
                var doc = XDocument.Parse(response);

                var upcomingEvents = doc.Descendants("event")
                    .Where(e => e.Element("impact")?.Value == "High" &&
                                currencies.Contains(e.Element("country")?.Value ?? ""))
                    .Select(e => new CalendarEvent
                    {
                        Title = e.Element("title")?.Value ?? "",
                        Country = e.Element("country")?.Value ?? "",
                        Date = e.Element("date")?.Value ?? "",
                        Time = e.Element("time")?.Value ?? "",
                        Impact = e.Element("impact")?.Value ?? "",
                        Forecast = e.Element("forecast")?.Value ?? "",
                        Previous = e.Element("previous")?.Value ?? ""
                    }).ToList();

                return upcomingEvents;
            }
            catch
            {
                return new List<CalendarEvent>();
            }
        });
    }

    // GENEL TAKVİM İÇİN
    public async Task<List<CalendarEvent>> GetAllHighImpactEventsAsync()
    {
        return await _cache.GetOrCreateAsync("CALENDAR_ALL", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);

            try
            {
                var response = await _httpClient.GetStringAsync("https://nfs.faireconomy.media/ff_calendar_thisweek.xml");
                var doc = XDocument.Parse(response);

                var upcomingEvents = doc.Descendants("event")
                    .Where(e => e.Element("impact")?.Value == "High")
                    .Select(e => new CalendarEvent
                    {
                        Title = e.Element("title")?.Value ?? "",
                        Country = e.Element("country")?.Value ?? "",
                        Date = e.Element("date")?.Value ?? "",
                        Time = e.Element("time")?.Value ?? "",
                        Impact = e.Element("impact")?.Value ?? "",
                        Forecast = e.Element("forecast")?.Value ?? "",
                        Previous = e.Element("previous")?.Value ?? ""
                    }).ToList();

                return upcomingEvents;
            }
            catch
            {
                return new List<CalendarEvent>();
            }
        });
    }

    // 2. WILLIAMS %R
    private async Task<double> GetWilliamsR(string symbol)
    {
        var candles = await _marketService.GetMarketCandlesAsync(symbol, "1d", 14);
        if (candles == null || candles.Count < 14) return -50.0;

        double highestHigh = candles.Max(c => (double)c.High);
        double lowestLow = candles.Min(c => (double)c.Low);
        double currentClose = (double)candles.Last().Close;

        double wpr = ((highestHigh - currentClose) / (highestHigh - lowestLow)) * -100.0;
        return Math.Round(wpr, 2);
    }

    // 3. YAPAY ZEKA ORKESTRASYONU
    public async Task<AiMacroReportResponse> GenerateTradeHypothesisAsync(string symbol)
    {
        // A. Sıkışma Verisini Getir
        var allSqueeze = await _squeezeService.GetAllSqueezeAnalysisAsync();
        var pairSqueeze = allSqueeze.FirstOrDefault(x => x.Symbol == symbol);

        string squeezeText = "Sıkışma yok.";
        if (pairSqueeze != null && pairSqueeze.SqueezedTfCount > 0)
        {
            squeezeText = $"{pairSqueeze.SqueezedTfCount} farklı zaman diliminde Bollinger Squeeze mevcut. En derin sıkışma: %{pairSqueeze.MinSqueezePercent}. {pairSqueeze.MaxSqueezeDuration} mumdur patlama bekliyor.";
        }

        // B. Haber Takvimini Getir
        var events = await GetUpcomingHighImpactEventsAsync(symbol);
        string eventsText = events.Any()
            ? string.Join(" | ", events.Select(e => $"[{e.Date} {e.Time}] {e.Country} {e.Title} (Beklenti: {e.Forecast})"))
            : "Önümüzdeki günlerde yüksek etkili haber yok.";

        // C. Sentiment (Williams %R) Getir
        double wpr = await GetWilliamsR(symbol);
        string sentimentText = wpr >= -20 ? "Aşırı Alım (Retail Long'da, Smart Money Satış arayabilir)"
                             : wpr <= -80 ? "Aşırı Satım (Retail Short'ta, Smart Money Alış arayabilir)"
                             : "Nötr Sentiment";

        // D. ÇİFT YÖNLÜ COT Verisini Getir (Base ve Quote Currency için)
        string baseCurrency = symbol.Substring(0, 3);
        string quoteCurrency = symbol.Substring(3, 3);

        string baseCotText = $"{baseCurrency} COT verisi bulunamadı.";
        string quoteCotText = $"{quoteCurrency} COT verisi bulunamadı.";

        try
        {
            var cotData = await _cotService.GetCotOverviewAsync();

            foreach (dynamic item in cotData)
            {
                string changeSign = item.WeeklyChange > 0 ? "+" : "";
                string weeklyChangeText = $"{changeSign}{item.WeeklyChange}";

                if (item.Currency == baseCurrency)
                {
                    baseCotText = $"{baseCurrency} Net Pozisyon: {item.CurrentNetPosition} (Haftalık: {weeklyChangeText} kontrat) | 6 Aylık: %{item.SixMonth.IndexScore} ({item.SixMonth.Bias}) | 12 Aylık: %{item.OneYear.IndexScore} ({item.OneYear.Bias})";
                }
                else if (item.Currency == quoteCurrency)
                {
                    quoteCotText = $"{quoteCurrency} Net Pozisyon: {item.CurrentNetPosition} (Haftalık: {weeklyChangeText} kontrat) | 6 Aylık: %{item.SixMonth.IndexScore} ({item.SixMonth.Bias}) | 12 Aylık: %{item.OneYear.IndexScore} ({item.OneYear.Bias})";
                }
            }
        }
        catch
        {
            // Veri çekilemezse sistem çökmesin
        }

        // E. Yapay Zekaya Gidecek Prompt'u Hazırla
        string prompt = $@"
Sen kıdemli bir kurumsal FX ve Makro analistisin. Bana '{symbol}' paritesi için aşağıdaki verilere dayanarak kısa, vurucu bir piyasa özeti yaz. HTML formatında ver (div, b, p, ul, li kullanabilirsin, markdown kullanma). Yorumun finansal piyasalarla yeni tanışanların bile anlayacağı bir ağzıyla olsun. 

KESİN KURALLAR:
1. KESİNLİKLE 'Aksiyon Planı' VEYA İŞLEM TAVSİYESİ VERME (Şuradan al, buradan sat, işlem fırsatı vb. kelimeler yasak).
2. Sadece durumu analiz et. {baseCurrency} ve {quoteCurrency} para birimlerinin COT (Kurumsal) verilerini BİRBİRİYLE KIYASLAYARAK (Divergence/Ayrışma) detaylı yorumla ve bunu paritedeki teknik sıkışma (Squeeze) ile bağdaştırarak bir volatilite öngörüsünde bulun.
3. Raporun en sonuna MUTLAKA '<p><br><i>⚠️ Yatırım tavsiyesi değildir!</i></p>' cümlesini ekle.

VERİLER:
1. Teknik Sıkışma (Squeeze - {symbol}): {squeezeText}
2. Yaklaşan Kritik Haberler ({baseCurrency} ve {quoteCurrency} Takvimi): {eventsText}
3. Bireysel Sentiment (Williams %R 14 - {symbol}): {sentimentText}
4. COT Kurumsal Pozisyonlanma (Kıyaslama İçin): 
   - {baseCotText}
   - {quoteCotText}

Çıktı Şablonu:
<div>
  <h3 style='color:#2962ff'>🤖 AI Makro Sentez: {symbol}</h3>
  <p><b>Kurumsal Ayrışma (COT) & Teknik Sıkışma:</b> (İki para biriminin COT kıyaslaması ve teknik durum)</p>
  <p><b>Katalizör (Tetikleyici):</b> (Haberlerin yaratabileceği etki)</p>
  <p><br><i>⚠️ Yatırım tavsiyesi değildir!</i></p>
</div>";

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        var serializeOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonString = JsonSerializer.Serialize(requestBody, serializeOptions);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        try
        {
            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_GEMINI_API_KEY"))
            {
                return new AiMacroReportResponse
                {
                    Symbol = symbol,
                    ReportHtml = "<div style='color:#ef5350'><b>API Anahtarı Yapılandırılmadı:</b> Lütfen appsettings.json dosyasına veya 'GEMINI_API_KEY' ortam değişkenine geçerli bir Google Gemini API anahtarı ekleyin.</div>"
                };
            }

            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}", content);

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AiMacroReportResponse
                {
                    Symbol = symbol,
                    ReportHtml = $"<div style='color:#ef5350'><b>API Hatası ({response.StatusCode}):</b><br><small>{responseString}</small></div>"
                };
            }

            using var jsonDoc = JsonDocument.Parse(responseString);
            var aiText = jsonDoc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            return new AiMacroReportResponse
            {
                Symbol = symbol,
                ReportHtml = aiText ?? "<p>Veri oluşturulamadı.</p>"
            };
        }
        catch (Exception ex)
        {
            return new AiMacroReportResponse
            {
                Symbol = symbol,
                ReportHtml = $"<div style='color:#ef5350'><b>Sistem Hatası:</b> {ex.Message}</div>"
            };
        }
    }
}