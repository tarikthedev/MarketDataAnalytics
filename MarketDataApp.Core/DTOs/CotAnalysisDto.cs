namespace MarketDataApp.Core.DTOs;

public class CotAnalysisDto
{
    public string Currency { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty; // YENİ: FX, Commodity, Bond
    public DateTime ReportDate { get; set; }
    public long CurrentNetPosition { get; set; }
    public int WeeklyChange { get; set; }
    public CotPeriodMetrics SixMonth { get; set; } = new();
    public CotPeriodMetrics OneYear { get; set; } = new();
    public CotPeriodMetrics TwoYear { get; set; } = new();
    public CotPeriodMetrics FourYear { get; set; } = new();
}

public class CotPeriodMetrics
{
    public long MinNet { get; set; }
    public long MaxNet { get; set; }
    public double IndexScore { get; set; }
    public string Bias { get; set; } = string.Empty;
}