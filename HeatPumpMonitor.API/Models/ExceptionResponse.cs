namespace HeatPumpMonitor.API.Models
{
    public class ExceptionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
