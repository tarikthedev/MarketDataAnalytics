using System;
using System.Collections.Generic;
using System.Text;

namespace MarketDataApp.Core.Entities;

public class CandleStick
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime OpenTime { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }
    public DateTime CloseTime { get; set; }
}