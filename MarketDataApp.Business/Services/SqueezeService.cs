using MarketDataApp.Core.DTOs;
using MarketDataApp.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MarketDataApp.Business.Services;

public class SqueezeTimeframeResult
{
    public string Timeframe { get; set; } = string.Empty;
    public double SqueezePercent { get; set; }
    public bool IsSqueezed { get; set; }
    public int SqueezeDurationBars { get; set; } // YENİ: Kaç bardır sıkışık?
    public double CurrentSpread { get; set; }
}

public class SqueezeMarketResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int SqueezedTfCount { get; set; }
    public double MinSqueezePercent { get; set; }
    public int MaxSqueezeDuration { get; set; } // En uzun sıkışma süresi
    public List<SqueezeTimeframeResult> Timeframes { get; set; } = new();
}

public class SqueezeService
{
    private readonly IMarketService _marketService;
    private readonly IMemoryCache _cache;

    private readonly List<(string Symbol, string Name, string Category)> _monitoredPairs = new()
    {
        ("EURUSD", "Euro / US Dollar", "Forex"),
        ("GBPUSD", "British Pound / US Dollar", "Forex"),
        ("USDJPY", "US Dollar / Japanese Yen", "Forex"),
        ("AUDUSD", "Australian Dollar / US Dollar", "Forex"),
        ("NZDUSD", "New Zealand Dollar / US Dollar", "Forex"),
        ("USDCAD", "US Dollar / Canadian Dollar", "Forex"),
        ("USDCHF", "US Dollar / Swiss Franc", "Forex"),
        ("XAUUSD", "Gold / US Dollar", "Metals"),
        ("XAGUSD", "Silver / US Dollar", "Metals"),
        ("BTCUSD", "Bitcoin / US Dollar", "Crypto"),
        ("US500",  "S&P 500 Index", "Indices"),
        ("USTECH", "Nasdaq 100", "Indices")
    };

    private readonly string[] _timeframes = { "1w", "1d", "4h", "1h" };

    public SqueezeService(IMarketService marketService, IMemoryCache cache)
    {
        _marketService = marketService;
        _cache = cache;
    }

    public async Task<List<SqueezeMarketResult>> GetAllSqueezeAnalysisAsync()
    {
        return await _cache.GetOrCreateAsync("ALL_SQUEEZE_ANALYSIS", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

            // HIZLANDIRMA ÇÖZÜMÜ: Task.Delay yerine SemaphoreSlim!
            // Aynı anda en fazla 3 parite isteği atarak Deriv'i spamlemeden maksimum hıza ulaşıyoruz.
            var semaphore = new SemaphoreSlim(3);
            var tasks = _monitoredPairs.Select(async p =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await AnalyzePairAsync(p.Symbol, p.Name, p.Category);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            // SIRALAMA: Önce en çok zaman diliminde sıkışanlar, sonra en uzun süre (mum) sıkışık kalanlar!
            return results
                .OrderByDescending(r => r.SqueezedTfCount)
                .ThenByDescending(r => r.MaxSqueezeDuration)
                .ThenBy(r => r.MinSqueezePercent)
                .ToList();
        }) ?? new List<SqueezeMarketResult>();
    }

    private async Task<SqueezeMarketResult> AnalyzePairAsync(string symbol, string name, string category)
    {
        var marketResult = new SqueezeMarketResult { Symbol = symbol, Name = name, Category = category };

        foreach (var tf in _timeframes)
        {
            try
            {
                var candles = await _marketService.GetMarketCandlesAsync(symbol, tf, 200);
                if (candles == null || candles.Count < 50) continue;

                var tfAnalysis = CalculateSqueezeOnly(candles, tf);
                marketResult.Timeframes.Add(tfAnalysis);
            }
            catch { /* Hata atla */ }
        }

        marketResult.SqueezedTfCount = marketResult.Timeframes.Count(t => t.IsSqueezed);
        marketResult.MinSqueezePercent = marketResult.Timeframes.Any() ? marketResult.Timeframes.Min(t => t.SqueezePercent) : 100.0;
        marketResult.MaxSqueezeDuration = marketResult.Timeframes.Any() ? marketResult.Timeframes.Max(t => t.SqueezeDurationBars) : 0;

        return marketResult;
    }

    private SqueezeTimeframeResult CalculateSqueezeOnly(List<CandleStickDTO> candles, string tf)
    {
        int count = candles.Count;
        var closes = candles.Select(c => (double)c.Close).ToList();

        // 1. Bollinger Bands Hesaplama
        int bbLength = 20;
        var bbBasis = CalculateSMA(closes, bbLength);
        var spreads = new List<double>(new double[count]);

        for (int i = 0; i < count; i++)
        {
            if (i < bbLength - 1) continue;
            var subCloses = closes.Skip(i - bbLength + 1).Take(bbLength).ToList();
            double mean = bbBasis[i];
            double variance = subCloses.Sum(c => Math.Pow(c - mean, 2)) / bbLength;
            double stdev = Math.Sqrt(variance);
            spreads[i] = (mean + 2.0 * stdev) - (mean - 2.0 * stdev);
        }

        if (count >= bbLength)
        {
            double firstValidSpread = spreads[bbLength - 1];
            for (int i = 0; i < bbLength - 1; i++) spreads[i] = firstValidSpread;
        }

        // 2. 100 SMA Spread ve Sıkışma Oranları
        int sqzLength = 100;
        var avgSpreads = CalculateSMA(spreads, sqzLength);
        var squeezePcts = new double[count];

        for (int i = 0; i < count; i++)
        {
            double avgSpread = avgSpreads[i] > 0 ? avgSpreads[i] : spreads[i];
            squeezePcts[i] = avgSpread > 0 ? (spreads[i] / avgSpread) * 100.0 : 100.0;
        }

        int lastIdx = count - 1;
        double currentSqueezePct = squeezePcts[lastIdx];
        bool isSqueezed = currentSqueezePct <= 50.0;

        // 3. YENİ MANTIK: Kaç bardır sıkışık hesapla
        int durationBars = 0;
        for (int i = lastIdx; i >= 0; i--)
        {
            if (squeezePcts[i] <= 50.0) durationBars++;
            else break;
        }

        return new SqueezeTimeframeResult
        {
            Timeframe = tf.ToUpper(),
            SqueezePercent = Math.Round(currentSqueezePct, 1),
            IsSqueezed = isSqueezed,
            SqueezeDurationBars = durationBars,
            CurrentSpread = Math.Round(spreads[lastIdx], 5)
        };
    }

    private List<double> CalculateSMA(List<double> values, int period)
    {
        var result = new List<double>(new double[values.Count]);
        double sum = 0;
        int currentPeriod = 0;

        for (int i = 0; i < values.Count; i++)
        {
            sum += values[i];
            currentPeriod++;

            if (currentPeriod > period)
            {
                sum -= values[i - period];
                currentPeriod = period;
            }
            result[i] = sum / currentPeriod;
        }
        return result;
    }
}