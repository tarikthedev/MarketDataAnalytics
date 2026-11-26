using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MarketDataApp.Core.Entities;
using MarketDataApp.Core.Interfaces;

namespace MarketDataApp.DataAccess.Clients;

public class DerivApiClient : IMarketApiClient
{
    private const string PrimaryWebSocketUrl = "wss://ws.derivws.com/websockets/v3?app_id=1089";
    private const string LegacyWebSocketUrl = "wss://ws.binaryws.com/websockets/v3?app_id=1089";

    private static readonly HttpClient _httpClient = CreateConfiguredHttpClient();

    private static HttpClient CreateConfiguredHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
        return client;
    }

    public async Task<List<CandleStick>> FetchCandlesFromExchangeAsync(string symbol, string interval, int limit)
    {
        // 1. Önce Deriv WebSocket üzerinden veri çekmeyi dene
        try
        {
            var derivCandles = await TryFetchFromDerivAsync(symbol, interval, limit);
            if (derivCandles != null && derivCandles.Count > 0)
            {
                return derivCandles;
            }
        }
        catch
        {
            // Deriv engelli veya erişilemez olduğunda sessizce alternatif sağlayıcıya (Yahoo Finance) geç
        }

        // 2. Deriv erişilemezse (BTK/SPK engeli, Cloudflare 520, WebSocket timeout vb.),
        // kurumsal Fallback olarak doğrudan güvenilir Yahoo Finance REST API'sinden canlı verileri al
        return await FetchCandlesFromYahooAsync(symbol, interval, limit);
    }

    private async Task<List<CandleStick>> TryFetchFromDerivAsync(string symbol, string interval, int limit)
    {
        string derivSymbol = symbol.ToUpperInvariant() switch
        {
            "BTCUSD" => "cryBTCUSD",
            "US500" => "OTC_SPC",
            "USTECH" => "OTC_NDX",
            "R_100" => "R_100",
            var s when s.StartsWith("XAU") || s.StartsWith("XAG") => $"frx{s}",
            var s => $"frx{s}"
        };

        int granularity = GetGranularitySeconds(interval);

        using var socket = new ClientWebSocket();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        string wsUrl = PrimaryWebSocketUrl;
        try
        {
            await socket.ConnectAsync(new Uri(wsUrl), timeout.Token);
        }
        catch
        {
            wsUrl = LegacyWebSocketUrl;
            await socket.ConnectAsync(new Uri(wsUrl), timeout.Token);
        }

        var request = new
        {
            ticks_history = derivSymbol,
            end = "latest",
            style = "candles",
            granularity = granularity,
            count = limit,
        };

        string json = JsonSerializer.Serialize(request);
        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), WebSocketMessageType.Text, true, timeout.Token);

        while (socket.State == WebSocketState.Open)
        {
            string responseJson = await ReceiveMessageAsync(socket, timeout.Token);
            if (string.IsNullOrWhiteSpace(responseJson)) continue;

            using JsonDocument document = JsonDocument.Parse(responseJson);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("error", out JsonElement error))
            {
                throw new Exception($"Deriv API hatası: {error.GetProperty("message").GetString()}");
            }

            if (!root.TryGetProperty("msg_type", out var msgType) || msgType.GetString() != "candles") continue;

            if (!root.TryGetProperty("candles", out var candlesElement)) return new List<CandleStick>();

            var result = new List<CandleStick>();
            foreach (var candle in candlesElement.EnumerateArray())
            {
                long epoch = candle.GetProperty("epoch").GetInt64();
                DateTime openTime = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;

                result.Add(new CandleStick
                {
                    Symbol = symbol,
                    Interval = interval,
                    OpenTime = openTime,
                    CloseTime = openTime.AddSeconds(granularity),
                    OpenPrice = candle.GetProperty("open").GetDecimal(),
                    HighPrice = candle.GetProperty("high").GetDecimal(),
                    LowPrice = candle.GetProperty("low").GetDecimal(),
                    ClosePrice = candle.GetProperty("close").GetDecimal(),
                    Volume = 0
                });
            }

            return result.OrderBy(x => x.OpenTime).ToList();
        }

        return new List<CandleStick>();
    }

    private async Task<List<CandleStick>> FetchCandlesFromYahooAsync(string symbol, string interval, int limit)
    {
        string yahooSymbol = MapToYahooSymbol(symbol);
        var (yahooInterval, yahooRange) = MapToYahooIntervalAndRange(interval);
        int granularity = GetGranularitySeconds(interval);

        string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?interval={yahooInterval}&range={yahooRange}";

        try
        {
            var responseJson = await _httpClient.GetStringAsync(url);
            using var document = JsonDocument.Parse(responseJson);

            var chart = document.RootElement.GetProperty("chart");
            if (!chart.TryGetProperty("result", out var resultArr) || resultArr.ValueKind == JsonValueKind.Null || resultArr.GetArrayLength() == 0)
            {
                return new List<CandleStick>();
            }

            var firstResult = resultArr[0];
            if (!firstResult.TryGetProperty("timestamp", out var timestamps) || timestamps.ValueKind != JsonValueKind.Array)
            {
                return new List<CandleStick>();
            }

            var quote = firstResult.GetProperty("indicators").GetProperty("quote")[0];
            var opens = quote.GetProperty("open");
            var highs = quote.GetProperty("high");
            var lows = quote.GetProperty("low");
            var closes = quote.GetProperty("close");
            var volumes = quote.TryGetProperty("volume", out var v) ? v : default;

            var candles = new List<CandleStick>();
            int count = timestamps.GetArrayLength();

            for (int i = 0; i < count; i++)
            {
                if (opens[i].ValueKind == JsonValueKind.Null ||
                    highs[i].ValueKind == JsonValueKind.Null ||
                    lows[i].ValueKind == JsonValueKind.Null ||
                    closes[i].ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                long epoch = timestamps[i].GetInt64();
                DateTime openTime = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;

                candles.Add(new CandleStick
                {
                    Symbol = symbol,
                    Interval = interval,
                    OpenTime = openTime,
                    CloseTime = openTime.AddSeconds(granularity),
                    OpenPrice = opens[i].GetDecimal(),
                    HighPrice = highs[i].GetDecimal(),
                    LowPrice = lows[i].GetDecimal(),
                    ClosePrice = closes[i].GetDecimal(),
                    Volume = (volumes.ValueKind == JsonValueKind.Array && volumes[i].ValueKind != JsonValueKind.Null)
                        ? volumes[i].GetDecimal()
                        : 0
                });
            }

            if (limit > 0 && candles.Count > limit)
            {
                candles = candles.Skip(candles.Count - limit).ToList();
            }

            return candles.OrderBy(x => x.OpenTime).ToList();
        }
        catch
        {
            return new List<CandleStick>();
        }
    }

    private static string MapToYahooSymbol(string symbol)
    {
        return symbol.ToUpperInvariant() switch
        {
            "BTCUSD" => "BTC-USD",
            "US500" => "^GSPC",
            "USTECH" => "^NDX",
            "R_100" => "^GSPC",
            "XAUUSD" => "GC=F",
            "XAGUSD" => "SI=F",
            var s => $"{s}=X"
        };
    }

    private static (string Interval, string Range) MapToYahooIntervalAndRange(string interval)
    {
        if (interval == "1M") return ("1mo", "10y");

        return interval.ToLowerInvariant() switch
        {
            "1m" => ("1m", "1d"),
            "2m" => ("2m", "30d"),
            "3m" => ("5m", "60d"),
            "5m" => ("5m", "60d"),
            "10m" => ("15m", "60d"),
            "15m" => ("15m", "60d"),
            "30m" => ("30m", "60d"),
            "1h" => ("60m", "730d"),
            "2h" => ("60m", "730d"),
            "4h" => ("60m", "730d"),
            "8h" => ("60m", "730d"),
            "1d" => ("1d", "5y"),
            "1w" => ("1wk", "10y"),
            _ => ("1d", "2y")
        };
    }

    private static int GetGranularitySeconds(string interval)
    {
        if (interval == "1M") return 2592000;

        return interval.ToLowerInvariant() switch
        {
            "1m" => 60,
            "2m" => 120,
            "3m" => 180,
            "5m" => 300,
            "10m" => 600,
            "15m" => 900,
            "30m" => 1800,
            "1h" => 3600,
            "2h" => 7200,
            "4h" => 14400,
            "8h" => 28800,
            "1d" => 86400,
            "1w" => 604800,
            _ => 86400
        };
    }

    private static async Task<string> ReceiveMessageAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        byte[] buffer = new byte[8192];
        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) return string.Empty;
            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage) break;
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
