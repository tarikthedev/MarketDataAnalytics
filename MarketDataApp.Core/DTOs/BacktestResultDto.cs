namespace MarketDataApp.Core.DTOs;

public class BacktestResultDto
{
    public int WeeksAgo { get; set; }
    public string ReportDate { get; set; }
    public string TradePeriod { get; set; } // YENİ: Hangi günler arası işleme girildi (Örn: 07 Eyl - 11 Eyl)
    public string Strongest { get; set; }
    public string Weakest { get; set; }
    public string Pair { get; set; }
    public string Bias { get; set; }
    public double EntryPrice { get; set; }
    public double ExitPrice { get; set; }
    public double ReturnPct { get; set; }
    public bool IsSuccess { get; set; }
}