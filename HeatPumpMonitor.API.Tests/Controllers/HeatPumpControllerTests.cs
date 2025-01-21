using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using HeatPumpMonitor.API.Controllers;
using HeatPumpMonitor.API.Interfaces;
using HeatPumpMonitor.API.Models;
using Xunit;

namespace HeatPumpMonitor.API.Tests.Controllers
{
    public class HeatPumpControllerTests
    {
        private readonly Mock<IScraperService> _mockScraperService;
        private readonly Mock<IAnalysisService> _mockAnalysisService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<HeatPumpController>> _mockLogger;
        private readonly HeatPumpController _controller;

        public HeatPumpControllerTests()
        {
            _mockScraperService = new Mock<IScraperService>();
            _mockAnalysisService = new Mock<IAnalysisService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<HeatPumpController>>();

            _controller = new HeatPumpController(
                _mockAnalysisService.Object,
                _mockConfiguration.Object,
                _mockScraperService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ScrapeHeatPumps_Success_ReturnsOkResult()
        {
            // Arrange
            var testProducts = new List<HeatPumpProduct>
            {
                new HeatPumpProduct
                {
                    Model = "Test Model 1",
                    ProductCode = "HP001",
                    Manufacturer = "Samsung",
                    Price = "£2,499.99",
                    Rating = 4.5,
                    ReviewCount = 15,
                    Features = new List<string> { "Feature 1", "Feature 2" },
                    IsEnergyEfficient = true,
                    Guarantee = "5 years"
                },
                new HeatPumpProduct
                {
                    Model = "Test Model 2",
                    ProductCode = "HP002",
                    Manufacturer = "Samsung",
                    Price = "£3,299.99",
                    Rating = 4.8,
                    ReviewCount = 23,
                    Features = new List<string> { "Feature 3", "Feature 4" },
                    IsEnergyEfficient = true,
                    Guarantee = "7 years"
                }
            };

            _mockScraperService
                .Setup(x => x.ScrapeHeatPumpsAsync())
                .ReturnsAsync(testProducts);

            _mockScraperService
                .Setup(x => x.SaveToCsvAsync(It.IsAny<List<HeatPumpProduct>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ScrapeHeatPumps();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<HeatPumpResponse>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(2, response.Count);
            Assert.Equal(testProducts, response.Products);

            _mockScraperService.Verify(x => x.ScrapeHeatPumpsAsync(), Times.Once);
            _mockScraperService.Verify(x => x.SaveToCsvAsync(testProducts), Times.Once);
        }

        [Fact]
        public async Task ScrapeHeatPumps_WhenScrapingFails_ReturnsInternalServerError()
        {
            // Arrange
            var expectedException = new Exception("Scraping failed");

            _mockScraperService
                .Setup(x => x.ScrapeHeatPumpsAsync())
                .ThrowsAsync(expectedException);

            // Act
            var result = await _controller.ScrapeHeatPumps();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var response = Assert.IsType<ExceptionResponse>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal(expectedException.Message, response.Message);

            VerifyLogError("Error scraping heat pump data");
        }

        [Fact]
        public async Task ScrapeHeatPumps_WhenSavingToCsvFails_ReturnsInternalServerError()
        {
            // Arrange
            var testProducts = new List<HeatPumpProduct>
            {
                new HeatPumpProduct
                {
                    Model = "Test Model 1",
                    ProductCode = "HP001"
                }
            };
            var expectedException = new Exception("CSV save failed");

            _mockScraperService
                .Setup(x => x.ScrapeHeatPumpsAsync())
                .ReturnsAsync(testProducts);

            _mockScraperService
                .Setup(x => x.SaveToCsvAsync(It.IsAny<List<HeatPumpProduct>>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _controller.ScrapeHeatPumps();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var response = Assert.IsType<ExceptionResponse>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal(expectedException.Message, response.Message);

            VerifyLogError("Error scraping heat pump data");
        }

        [Fact]
        public async Task GetSummary_Success_ReturnsOkResult()
        {
            // Arrange
            var csvPath = "test.csv";
            var expectedSummary = new HeatPumpSummary
            {
                TotalProducts = 2,
                AveragePrice = 2899.99m,
                AverageRating = 4.65,
                TotalReviews = 38,
                EnergyEfficientCount = 2,
                TopFeatures = new Dictionary<string, int>
                {
                    { "Feature 1", 2 },
                    { "Feature 2", 1 }
                },
                ManufacturerCount = new Dictionary<string, int>
                {
                    { "Samsung", 2 }
                }
            };

            _mockConfiguration
                .Setup(x => x["CsvFilePath"])
                .Returns(csvPath);

            _mockAnalysisService
                .Setup(x => x.GenerateSummaryAsync(csvPath))
                .ReturnsAsync(expectedSummary);

            // Act
            var result = await _controller.GetSummary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var summary = Assert.IsType<HeatPumpSummary>(okResult.Value);

            Assert.Equal(expectedSummary.TotalProducts, summary.TotalProducts);
            Assert.Equal(expectedSummary.AveragePrice, summary.AveragePrice);
            Assert.Equal(expectedSummary.AverageRating, summary.AverageRating);
            Assert.Equal(expectedSummary.TotalReviews, summary.TotalReviews);
            Assert.Equal(expectedSummary.TopFeatures, summary.TopFeatures);
            Assert.Equal(expectedSummary.ManufacturerCount, summary.ManufacturerCount);
        }

        [Fact]
        public async Task GetSummary_WhenAnalysisFails_ReturnsInternalServerError()
        {
            // Arrange
            var expectedException = new Exception("Analysis failed");

            _mockConfiguration
                .Setup(x => x["CsvFilePath"])
                .Returns("test.csv");

            _mockAnalysisService
                .Setup(x => x.GenerateSummaryAsync(It.IsAny<string>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _controller.GetSummary();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var response = Assert.IsType<ExceptionResponse>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal(expectedException.Message, response.Message);

            VerifyLogError("Error generating summary");
        }

        private void VerifyLogError(string message)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}

