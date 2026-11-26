using MarketDataApp.Core.DTOs;
using MarketDataApp.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MarketDataApp.Business.Services;

public class MarketService : IMarketService
{
    private readonly IMarketApiClient _apiClient;
    private readonly IMemoryCache _cache;

    public MarketService(
        IMarketApiClient apiClient,
        IMemoryCache cache)
    {
        _apiClient = apiClient;
        _cache = cache;
    }

    public async Task<List<CandleStickDTO>> GetMarketCandlesAsync(
        string symbol,
        string interval,
        int limit)
    {
        // 1m (Dakika) ile 1M (Ay) karışmaması için cache anahtarı harf duyarlıdır
        string cacheKey = $"CANDLES_{symbol.ToUpper()}_{interval}_{limit}";

        if (_cache.TryGetValue(cacheKey, out List<CandleStickDTO>? cached) && cached != null)
        {
            return cached;
        }

        // Haftalık ve Aylık kontrolü
        bool isWeekly = string.Equals(interval, "1w", StringComparison.OrdinalIgnoreCase);
        bool isMonthly = interval == "1M"; // Büyük M = Aylık

        // Eğer 1W veya 1M ise Deriv'den 1d (günlük) verileri çekip birleştireceğiz
        string fetchInterval = (isWeekly || isMonthly) ? "1d" : interval;
        int fetchLimit = (isWeekly || isMonthly) ? 5000 : limit;

        var raw = await _apiClient.FetchCandlesFromExchangeAsync(
            symbol,
            fetchInterval,
            fetchLimit);

        if (raw == null || raw.Count == 0)
        {
            return new List<CandleStickDTO>();
        }

        var result = raw.Select(candle => new CandleStickDTO
        {
            Time = new DateTimeOffset(candle.OpenTime).ToUnixTimeSeconds(),
            Open = candle.OpenPrice,
            High = candle.HighPrice,
            Low = candle.LowPrice,
            Close = candle.ClosePrice,
            Volume = candle.Volume
        })
        .OrderBy(x => x.Time)
        .ToList();

        // Gelen günlük veriyi Haftalık veya Aylık mumlara dönüştür
        if (isWeekly)
        {
            result = AggregateToWeekly(result);
        }
        else if (isMonthly)
        {
            result = AggregateToMonthly(result);
        }

        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));

        return result;
    }

    // HAFTALIK BİRLEŞTİRME (Pazartesi 00:00 UTC başlangıçlı)
    private List<CandleStickDTO> AggregateToWeekly(List<CandleStickDTO> dailyCandles)
    {
        if (dailyCandles == null || !dailyCandles.Any()) return new List<CandleStickDTO>();

        var groups = dailyCandles
            .Select(c => new
            {
                Candle = c,
                Date = DateTimeOffset.FromUnixTimeSeconds(c.Time).UtcDateTime
            })
            .GroupBy(x => GetMondayOfWeek(x.Date));

        var result = new List<CandleStickDTO>();
        foreach (var group in groups)
        {
            var candles = group.OrderBy(x => x.Candle.Time).Select(x => x.Candle).ToList();
            result.Add(new CandleStickDTO
            {
                Time = new DateTimeOffset(group.Key, TimeSpan.Zero).ToUnixTimeSeconds(),
                Open = candles.First().Open,
                High = candles.Max(c => c.High),
                Low = candles.Min(c => c.Low),
                Close = candles.Last().Close,
                Volume = candles.Sum(c => c.Volume)
            });
        }

        return result.OrderBy(x => x.Time).ToList();
    }

    // AYLIK BİRLEŞTİRME (Ayın 1'i 00:00 UTC başlangıçlı)
    private List<CandleStickDTO> AggregateToMonthly(List<CandleStickDTO> dailyCandles)
    {
        if (dailyCandles == null || !dailyCandles.Any()) return new List<CandleStickDTO>();

        var groups = dailyCandles
            .Select(c => new
            {
                Candle = c,
                Date = DateTimeOffset.FromUnixTimeSeconds(c.Time).UtcDateTime
            })
            .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = new List<CandleStickDTO>();
        foreach (var group in groups)
        {
            var candles = group.OrderBy(x => x.Candle.Time).Select(x => x.Candle).ToList();
            result.Add(new CandleStickDTO
            {
                Time = new DateTimeOffset(group.Key, TimeSpan.Zero).ToUnixTimeSeconds(),
                Open = candles.First().Open,
                High = candles.Max(c => c.High),
                Low = candles.Min(c => c.Low),
                Close = candles.Last().Close,
                Volume = candles.Sum(c => c.Volume)
            });
        }

        return result.OrderBy(x => x.Time).ToList();
    }

    private static DateTime GetMondayOfWeek(DateTime dt)
    {
        int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dt.Date.AddDays(-1 * diff);
    }
}