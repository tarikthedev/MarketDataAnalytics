using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MarketDataApp.Core.Entities;

namespace MarketDataApp.Core.Interfaces
{
    public interface IMarketApiClient
    {
        Task<List<CandleStick>> FetchCandlesFromExchangeAsync(string symbol, string interval, int limit);
    }
}
