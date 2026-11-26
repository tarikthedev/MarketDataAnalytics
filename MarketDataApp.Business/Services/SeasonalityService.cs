using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MarketDataApp.Business.Services;

public class SeasonalityService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public SeasonalityService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }

    public async Task<object> GetSeasonalityAsync(string symbol)
    {
        return await _cache.GetOrCreateAsync($"SEASONALITY_{symbol}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

            // Yahoo Finance Sembol Eşleştirmesi
            string yahooSymbol = symbol switch
            {
                "EURUSD" => "EURUSD=X",
                "GBPUSD" => "GBPUSD=X",
                "USDJPY" => "JPY=X",
                "AUDUSD" => "AUDUSD=X",
                "NZDUSD" => "NZDUSD=X",
                "XAUUSD" => "GC=F", // Altın Vadeli
                "XAGUSD" => "SI=F", // Gümüş Vadeli
                "BTCUSD" => "BTC-USD",
                "US500" => "^GSPC", // S&P 500
                "USTECH" => "^IXIC", // Nasdaq
                _ => $"{symbol}=X"
            };

            // Yahoo'dan son 20 yılın Günlük verisini çekiyoruz
            string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooSymbol}?interval=1d&range=20y";
            var response = await _httpClient.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];

            var timestamps = result.GetProperty("timestamp").EnumerateArray().Select(x => x.GetInt64()).ToList();

            // DÜZELTİLEN KISIM: Verileri direkt double yapmadan önce ham (Element) olarak alıyoruz
            var closesElements = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close").EnumerateArray().ToList();

            var dailyData = new List<(DateTime Date, double Close)>();
            for (int i = 0; i < timestamps.Count; i++)
            {
                // YENİ NULL KONTROLÜ: Eğer o gün piyasa tatilse veya Yahoo null gönderdiyse pas geç.
                if (closesElements[i].ValueKind == JsonValueKind.Number)
                {
                    double closePrice = closesElements[i].GetDouble();
                    if (closePrice > 0)
                    {
                        var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime.Date;
                        dailyData.Add((date, closePrice));
                    }
                }
            }

            // Yıllara göre grupla
            var groupedByYear = dailyData.GroupBy(x => x.Date.Year).ToDictionary(g => g.Key, g => g.ToList());
            int currentYear = DateTime.UtcNow.Year;

            // 5, 10 ve 20 yıllık ortalama YTD (Year-To-Date) getirilerini hesapla
            var avg5Y = CalculateSeasonalAverage(groupedByYear, currentYear, 5);
            var avg10Y = CalculateSeasonalAverage(groupedByYear, currentYear, 10);
            var avg20Y = CalculateSeasonalAverage(groupedByYear, currentYear, 20);

            return new
            {
                Symbol = symbol,
                Data = new { FiveYear = avg5Y, TenYear = avg10Y, TwentyYear = avg20Y }
            };
        });
    }

    private Dictionary<string, double> CalculateSeasonalAverage(Dictionary<int, List<(DateTime Date, double Close)>> yearlyData, int currentYear, int yearsBack)
    {
        var result = new Dictionary<string, double>();
        // Hedef yılları belirle (Örn: şu an 2026 ise son 10 yıl için 2016-2025 arasını alır)
        var targetYears = yearlyData.Keys.Where(y => y >= currentYear - yearsBack && y < currentYear).ToList();

        if (!targetYears.Any()) return result;

        // Yılın her gününü (Ay-Gün formatında) tarayıp ortalama alıyoruz
        for (int month = 1; month <= 12; month++)
        {
            for (int day = 1; day <= 31; day++)
            {
                // Artık yıllardaki Şubat 29'u kaybetmemek için referans olarak 2024'ü (artık yıl) kullanıyoruz.
                if (day > DateTime.DaysInMonth(2024, month)) continue;

                string dateKey = $"{month:D2}-{day:D2}";
                double sumReturn = 0;
                int count = 0;

                foreach (var year in targetYears)
                {
                    var data = yearlyData[year];
                    if (!data.Any()) continue;

                    double startOfYearPrice = data.First().Close;

                    // O güne en yakın (öncesindeki) fiyatı bul (Hafta sonu/Tatil boşluklarını doldurmak için)
                    var dayData = data.LastOrDefault(d => d.Date.Month <= month && (d.Date.Month < month || d.Date.Day <= day));

                    if (startOfYearPrice > 0 && dayData.Close > 0)
                    {
                        double percentChange = ((dayData.Close - startOfYearPrice) / startOfYearPrice) * 100;
                        sumReturn += percentChange;
                        count++;
                    }
                }

                if (count > 0)
                {
                    result[dateKey] = Math.Round(sumReturn / count, 3);
                }
            }
        }
        return result;
    }
}