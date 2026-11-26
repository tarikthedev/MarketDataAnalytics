using System;
using System.Collections.Generic;
using System.Text;

namespace MarketDataApp.Core.DTOs;

public class CotHistoryDto
{
    public string Time { get; set; } // TradingView "time" formatı bekler (YYYY-MM-DD)
    public double Value { get; set; } // TradingView "value" formatı bekler
}