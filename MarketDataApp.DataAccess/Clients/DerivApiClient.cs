using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MarketDataApp.Core.Entities;
using MarketDataApp.Core.Interfaces;

namespace MarketDataApp.DataAccess.Clients;

public class DerivApiClient : IMarketApiClient
{
    private const string WebSocketUrl = "wss://ws.binaryws.com/websockets/v3?app_id=1089";

    public async Task<List<CandleStick>> FetchCandlesFromExchangeAsync(string symbol, string interval, int limit)
    {
        // Yeni Deriv sembol eşleştirmeleri
        string derivSymbol = symbol.ToUpperInvariant() switch
        {
            "BTCUSD" => "cryBTCUSD",
            "US500" => "OTC_SPC", 
            "USTECH" => "OTC_NDX", 
            "R_100" => "R_100",
            var s when s.StartsWith("XAU") || s.StartsWith("XAG") => $"frx{s}",
            var s => $"frx{s}" // Majör Forex'ler (EURUSD, NZDUSD, AUDUSD vb.)
        };

        int granularity = interval.ToLowerInvariant() switch
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
            _ => throw new ArgumentException($"Desteklenmeyen interval: {interval}")
        };

        using var socket = new ClientWebSocket();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await socket.ConnectAsync(new Uri(WebSocketUrl), timeout.Token);

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