using System;
using System.Collections.Generic;
using System.Text;

namespace MarketDataApp.Core.DTOs
{
    public class CandleStickDTO
    {
        public long Time {  get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
        public decimal Volume { get; set; }
    }
}
