using HeatPumpMonitor.API.Models;

namespace HeatPumpMonitor.API.Interfaces
{
    public interface IAnalysisService
    {
        Task<HeatPumpSummary> GenerateSummaryAsync(string csvPath);
    }
}
