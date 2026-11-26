using System.Text.Json;
using MarketDataApp.Core.DTOs;
using MarketDataApp.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MarketDataApp.Business.Services;

public class CotService : ICotService
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    public CotService(IMemoryCache cache, HttpClient httpClient)
    {
        _cache = cache;
        _httpClient = httpClient;
    }

    public async Task<List<CotAnalysisDto>> GetCotOverviewAsync(int weeksAgo = 0)
    {
        return await _cache.GetOrCreateAsync($"COT_DATA_OVERVIEW_{weeksAgo}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

            // Kod, İsim ve GRUP (FX, Commodity, Bond)
            var cftcCodes = new Dictionary<string, (string Code, string Name, string Group)>
            {
                // DÖVİZLER
                { "EUR", ("099741", "EURO FX", "FX") },
                { "GBP", ("096742", "BRITISH POUND", "FX") },
                { "JPY", ("097741", "JAPANESE YEN", "FX") },
                { "AUD", ("232741", "AUSTRALIAN DOLLAR", "FX") },
                { "NZD", ("112741", "NEW ZEALAND DOLLAR", "FX") },
                { "CAD", ("090741", "CANADIAN DOLLAR", "FX") },
                { "CHF", ("092741", "SWISS FRANC", "FX") },
                { "USD", ("098662", "USD INDEX", "FX") },
                { "ZAR", ("122741", "SOUTH AFRICAN RAND", "FX") },
                { "MXN", ("095741", "MEXICAN PESO", "FX") },
                { "BRL", ("102741", "BRAZILIAN REAL", "FX") },
                
                // EMTİALAR
                { "CL", ("067651", "CRUDE OIL", "Commodity") },
                { "GC", ("088691", "GOLD", "Commodity") },
                { "SI", ("084691", "SILVER", "Commodity") },
                
                // TAHVİLLER (YENİ)
                { "ZB", ("020601", "30-YEAR U.S. TREASURY BONDS", "Bond") },
                { "ZN", ("043602", "10-YEAR U.S. TREASURY NOTES", "Bond") },
                { "ZF", ("044601", "5-YEAR U.S. TREASURY NOTES", "Bond") }
            };

            var result = new List<CotAnalysisDto>();

            foreach (var kvp in cftcCodes)
            {
                var historyWeeklyNet = await FetchCftcSocrataDataAsync(kvp.Value.Code);
                if (historyWeeklyNet.Count == 0) continue;

                var sortedHistory = historyWeeklyNet.OrderBy(x => x.Date).ToList();

                int targetIndex = sortedHistory.Count - 1 - weeksAgo;
                if (targetIndex < 0) continue;

                long currentNet = sortedHistory[targetIndex].Net;
                long previousNet = targetIndex > 0 ? sortedHistory[targetIndex - 1].Net : currentNet;
                int weeklyChange = (int)(currentNet - previousNet);

                var validHistory = sortedHistory.Take(targetIndex + 1).Select(x => x.Net).ToList();

                var net6m = validHistory.TakeLast(Math.Min(26, validHistory.Count)).ToList();
                var net12m = validHistory.TakeLast(Math.Min(52, validHistory.Count)).ToList();
                var net24m = validHistory.TakeLast(Math.Min(104, validHistory.Count)).ToList();
                var net48m = validHistory.TakeLast(Math.Min(208, validHistory.Count)).ToList();

                result.Add(new CotAnalysisDto
                {
                    Currency = kvp.Key,
                    ContractName = kvp.Value.Name,
                    Group = kvp.Value.Group, // GRUP BİLGİSİ EKLENDİ
                    ReportDate = sortedHistory[targetIndex].Date,
                    CurrentNetPosition = currentNet,
                    WeeklyChange = weeklyChange,
                    SixMonth = CalculateMetrics(currentNet, net6m),
                    OneYear = CalculateMetrics(currentNet, net12m),
                    TwoYear = CalculateMetrics(currentNet, net24m),
                    FourYear = CalculateMetrics(currentNet, net48m)
                });
            }
            return result;
        }) ?? new List<CotAnalysisDto>();
    }

    private async Task<List<(DateTime Date, long Net)>> FetchCftcSocrataDataAsync(string cftcCode)
    {
        try
        {
            string url = $"https://publicreporting.cftc.gov/resource/6dca-aqww.json?$where=cftc_contract_market_code='{cftcCode}'&$order=report_date_as_yyyy_mm_dd DESC&$limit=300";
            string response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            var list = new List<(DateTime Date, long Net)>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("comm_positions_long_all", out var longProp) &&
                    element.TryGetProperty("comm_positions_short_all", out var shortProp) &&
                    element.TryGetProperty("report_date_as_yyyy_mm_dd", out var dateProp))
                {
                    if (long.TryParse(longProp.GetString(), out long cLong) &&
                        long.TryParse(shortProp.GetString(), out long cShort) &&
                        DateTime.TryParse(dateProp.GetString(), out DateTime date))
                    {
                        list.Add((date, cLong - cShort));
                    }
                }
            }
            return list;
        }
        catch { return new List<(DateTime, long)>(); }
    }

    private static CotPeriodMetrics CalculateMetrics(long current, List<long> history)
    {
        if (history.Count <= 1)
            return new CotPeriodMetrics { MinNet = current, MaxNet = current, IndexScore = 50.0, Bias = "Nötr" };

        long min = history.Min();
        long max = history.Max();
        double score = (max == min) ? 50.0 : ((double)(current - min) / (max - min)) * 100.0;
        score = Math.Clamp(score, 0.0, 100.0);

        string bias = score switch
        {
            >= 75 => "Güçlü Yükseliş",
            >= 55 => "Yükseliş",
            <= 25 => "Güçlü Düşüş",
            <= 45 => "Düşüş",
            _ => "Nötr"
        };

        return new CotPeriodMetrics { MinNet = min, MaxNet = max, IndexScore = Math.Round(score, 1), Bias = bias };
    }

    public async Task<List<CotHistoryDto>> GetCotHistoryAsync(string currency)
    {
        var cftcCodes = new Dictionary<string, string>
        {
            { "EUR", "099741" }, { "GBP", "096742" }, { "JPY", "097741" }, { "AUD", "232741" },
            { "NZD", "112741" }, { "CAD", "090741" }, { "CHF", "092741" }, { "USD", "098662" },
            { "ZAR", "122741" }, { "MXN", "095741" }, { "BRL", "102741" },
            { "CL", "067651" }, { "GC", "088691" }, { "SI", "084691" },
            // TAHVİLLER (YENİ)
            { "ZB", "020601" }, { "ZN", "043602" }, { "ZF", "044601" }
        };

        if (!cftcCodes.ContainsKey(currency)) return new List<CotHistoryDto>();

        return await _cache.GetOrCreateAsync($"COT_HISTORY_{currency}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            var rawData = await FetchCftcSocrataDataAsync(cftcCodes[currency]);
            return rawData.OrderBy(x => x.Date).Select(x => new CotHistoryDto
            {
                Time = x.Date.ToString("yyyy-MM-dd"),
                Value = x.Net
            }).ToList();
        }) ?? new List<CotHistoryDto>();
    }

    private (string Pair, string Bias) GetStandardPair(string strong, string weak)
    {
        var hierarchy = new List<string> { "EUR", "GBP", "AUD", "NZD", "USD", "CAD", "CHF", "JPY", "ZAR", "MXN", "BRL" };
        int strongIdx = hierarchy.IndexOf(strong);
        int weakIdx = hierarchy.IndexOf(weak);

        if (strongIdx == -1 || weakIdx == -1) return ($"{strong}{weak}", "LONG");
        if (strongIdx < weakIdx) return ($"{strong}{weak}", "LONG");
        else return ($"{weak}{strong}", "SHORT");
    }

    private async Task<(double Entry, double Exit, string EntryDateStr, string ExitDateStr)> GetHistoricalPricesAsync(string pair, DateTime reportDate)
    {
        string yahooSymbol = $"{pair}=X";
        var chartData = await _cache.GetOrCreateAsync($"YAHOO_5Y_OHLC_{yahooSymbol}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooSymbol}?interval=1d&range=5y";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");
            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var timestamps = result.GetProperty("timestamp").EnumerateArray().Select(x => x.GetInt64()).ToList();
            var quote = result.GetProperty("indicators").GetProperty("quote")[0];
            var opens = quote.GetProperty("open").EnumerateArray().ToList();
            var closes = quote.GetProperty("close").EnumerateArray().ToList();

            var prices = new Dictionary<DateTime, (double Open, double Close)>();
            for (int i = 0; i < timestamps.Count; i++)
            {
                if (closes[i].ValueKind == JsonValueKind.Number && opens[i].ValueKind == JsonValueKind.Number)
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime.Date;
                    prices[date] = (opens[i].GetDouble(), closes[i].GetDouble());
                }
            }
            return prices;
        });

        DateTime tradeMonday = reportDate.Date;
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)tradeMonday.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        tradeMonday = tradeMonday.AddDays(daysUntilMonday);
        DateTime tradeFriday = tradeMonday.AddDays(4);

        while (!chartData.ContainsKey(tradeMonday) && tradeMonday <= tradeFriday) tradeMonday = tradeMonday.AddDays(1);
        while (!chartData.ContainsKey(tradeFriday) && tradeFriday >= tradeMonday) tradeFriday = tradeFriday.AddDays(-1);

        double entryPrice = chartData.ContainsKey(tradeMonday) ? chartData[tradeMonday].Open : 0;
        double exitPrice = chartData.ContainsKey(tradeFriday) ? chartData[tradeFriday].Close : 0;

        return (entryPrice, exitPrice, tradeMonday.ToString("dd MMM"), tradeFriday.ToString("dd MMM"));
    }

    public async Task<List<BacktestResultDto>> RunBacktestAsync(int weeksToTest = 10)
    {
        var results = new List<BacktestResultDto>();

        for (int w = 1; w <= weeksToTest; w++)
        {
            var weeklyData = await GetCotOverviewAsync(w);
            if (weeklyData == null || weeklyData.Count == 0) continue;

            // SADECE "FX" GRUBUNU FİLTRELE (Tahvil veya Emtialar birbirleriyle cross parite yapılmaz)
            var fxData = weeklyData.Where(x => x.Group == "FX").ToList();
            if (fxData.Count < 2) continue;

            var sortedData = fxData.OrderByDescending(x => x.SixMonth.IndexScore)
                                   .ThenByDescending(x => x.OneYear.IndexScore)
                                   .ThenByDescending(x => x.TwoYear.IndexScore)
                                   .ThenByDescending(x => x.FourYear.IndexScore)
                                   .ToList();

            var strongest = sortedData.First();
            var weakest = sortedData.Last();

            var (pair, bias) = GetStandardPair(strongest.Currency, weakest.Currency);
            var (entryPrice, exitPrice, entryDateStr, exitDateStr) = await GetHistoricalPricesAsync(pair, strongest.ReportDate);

            if (entryPrice > 0 && exitPrice > 0)
            {
                double rawReturn = ((exitPrice - entryPrice) / entryPrice) * 100;
                double finalReturn = bias == "LONG" ? rawReturn : -rawReturn;

                results.Add(new BacktestResultDto
                {
                    WeeksAgo = w,
                    ReportDate = strongest.ReportDate.ToString("dd MMM yyyy"),
                    TradePeriod = $"{entryDateStr} - {exitDateStr}",
                    Strongest = strongest.Currency,
                    Weakest = weakest.Currency,
                    Pair = pair,
                    Bias = bias,
                    EntryPrice = Math.Round(entryPrice, 4),
                    ExitPrice = Math.Round(exitPrice, 4),
                    ReturnPct = Math.Round(finalReturn, 2),
                    IsSuccess = finalReturn > 0
                });
            }
        }
        return results;
    }
}