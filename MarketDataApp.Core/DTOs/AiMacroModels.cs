namespace MarketDataApp.Core.DTOs;

public class CalendarEvent
{
    public string Title { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Forecast { get; set; } = string.Empty;
    public string Previous { get; set; } = string.Empty;
}

public class AiMacroReportResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string ReportHtml { get; set; } = string.Empty;
}