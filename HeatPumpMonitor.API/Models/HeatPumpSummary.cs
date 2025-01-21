namespace HeatPumpMonitor.API.Models
{
    public class HeatPumpSummary
    {
        public int TotalProducts { get; set; }
        public decimal AveragePrice { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int EnergyEfficientCount { get; set; }
        public Dictionary<string, int> TopFeatures { get; set; } = [];
        public Dictionary<string, int> ManufacturerCount { get; set; } = [];
        public DateTime DateGenerated { get; set; }
    }
}
