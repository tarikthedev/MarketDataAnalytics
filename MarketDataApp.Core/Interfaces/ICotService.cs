using MarketDataApp.Core.DTOs;

namespace MarketDataApp.Core.Interfaces;

public interface ICotService
{
    Task<List<CotAnalysisDto>> GetCotOverviewAsync(int weeksAgo = 0);

    Task<List<CotHistoryDto>> GetCotHistoryAsync(string currency);

    Task<List<BacktestResultDto>> RunBacktestAsync(int weeksToTest = 10);
}