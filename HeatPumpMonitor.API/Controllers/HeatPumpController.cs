using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HeatPumpMonitor.API.Interfaces;
using HeatPumpMonitor.API.Models;

namespace HeatPumpMonitor.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HeatPumpController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IScraperService _scraperService;
        private readonly ILogger<HeatPumpController> _logger;
        private readonly IAnalysisService _analysisService;

        public HeatPumpController(
            IAnalysisService analysisService,
            IConfiguration configuration,
            IScraperService scraperService,
            ILogger<HeatPumpController> logger)
        {
            _analysisService = analysisService;
            _configuration = configuration;
            _scraperService = scraperService;
            _logger = logger;
        }

        [HttpGet("scrape")]
        public async Task<IActionResult> ScrapeHeatPumps()
        {
            try
            {
                var products = await _scraperService.ScrapeHeatPumpsAsync();
                await _scraperService.SaveToCsvAsync(products);

                return Ok(new HeatPumpResponse
                {
                    Success = true,
                    Count = products.Count,
                    Products = products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping heat pump data");
                return StatusCode(500, new ExceptionResponse{ Success = false, Message = ex.Message });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var csvPath = _configuration["CsvFilePath"] ?? string.Empty;
                var summary = await _analysisService.GenerateSummaryAsync(csvPath);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary");
                return StatusCode(500, new ExceptionResponse { Success = false, Message = ex.Message });
            }
        }
    }
}
