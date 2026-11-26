using System;
using System.Collections.Generic;
using System.Text;
using MarketDataApp.Core.DTOs;

namespace MarketDataApp.Core.Interfaces
{
    public interface IMarketService
    {
        Task<List<CandleStickDTO>> GetMarketCandlesAsync(string symbol, string interval, int limit);
    }
}
