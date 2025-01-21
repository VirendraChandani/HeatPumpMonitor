namespace HeatPumpMonitor.API.Models
{
    public class HeatPumpResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<HeatPumpProduct> Products { get; set; }
    }
}
