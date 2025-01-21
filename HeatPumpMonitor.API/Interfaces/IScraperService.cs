using HeatPumpMonitor.API.Models;

namespace HeatPumpMonitor.API.Interfaces
{
    public interface IScraperService
    {
        Task<List<HeatPumpProduct>> ScrapeHeatPumpsAsync();
        Task SaveToCsvAsync(List<HeatPumpProduct> products);
    }
}
