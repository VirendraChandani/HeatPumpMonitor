using CsvHelper;
using HeatPumpMonitor.API.Interfaces;
using HeatPumpMonitor.API.Models;
using System.Globalization;

namespace HeatPumpMonitor.API.Services
{
    public class AnalysisService : IAnalysisService
    {
        private readonly ILogger<AnalysisService> _logger;

        public AnalysisService(ILogger<AnalysisService> logger)
        {
            _logger = logger;
        }

        public async Task<HeatPumpSummary> GenerateSummaryAsync(string csvPath)
        {
            try
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                var records = new List<HeatPumpProduct>();
                await foreach (var record in csv.GetRecordsAsync<HeatPumpProduct>())
                {
                    records.Add(record);
                }

                if (records.Count == 0)
                    return new HeatPumpSummary { DateGenerated = DateTime.UtcNow };

                var summary = new HeatPumpSummary
                {
                    TotalProducts = records.Count,
                    DateGenerated = DateTime.UtcNow,
                    AveragePrice = CalculateAveragePrice(records),
                    AverageRating = records.Average(r => r.Rating),
                    TotalReviews = records.Sum(r => r.ReviewCount),
                    EnergyEfficientCount = records.Count(r => r.IsEnergyEfficient),
                    TopFeatures = GetTopFeatures(records, 5),
                    ManufacturerCount = records.GroupBy(r => r.Manufacturer).ToDictionary(g => g.Key, g => g.Count())
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating heat pump summary");
                throw;
            }
        }

        private decimal CalculateAveragePrice(List<HeatPumpProduct> products)
        {
            var prices = products.Select(p => ParsePrice(p.Price))
                               .Where(p => p.HasValue)
                               .Select(p => p.Value);

            return prices.Any() ? prices.Average() : 0;
        }

        private decimal? ParsePrice(string price)
        {
            if (string.IsNullOrEmpty(price)) return null;

            var cleanPrice = price.Replace("£", "")
                                .Replace(",", "")
                                .Replace("Inc Vat", "")
                                .Trim();

            return decimal.TryParse(cleanPrice, out decimal result) ? result : null;
        }

        private Dictionary<string, int> GetTopFeatures(List<HeatPumpProduct> products, int topCount)
        {
            return products.SelectMany(p => p.Features)
                          .GroupBy(f => f)
                          .OrderByDescending(g => g.Count())
                          .Take(topCount)
                          .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
