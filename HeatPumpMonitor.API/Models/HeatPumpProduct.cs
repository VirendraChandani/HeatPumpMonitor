using CsvHelper.Configuration.Attributes;
using HeatPumpMonitor.API.Converters;

namespace HeatPumpMonitor.API.Models
{
    public class HeatPumpProduct
    {
        public string Model { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        [Name("Price (inc VAT)")]
        public string Price { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        [TypeConverter(typeof(FeaturesConverter))]
        public List<string> Features { get; set; } = [];
        public bool IsEnergyEfficient { get; set; }
        public string Guarantee { get; set; } = string.Empty;
    }
}
