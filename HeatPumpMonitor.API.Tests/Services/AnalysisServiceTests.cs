using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HeatPumpMonitor.API.Models;
using HeatPumpMonitor.API.Services;
using System.Globalization;

namespace HeatPumpMonitor.API.Tests.Services
{
    public class AnalysisServiceTests
    {
        private readonly Mock<ILogger<AnalysisService>> _mockLogger;
        private readonly AnalysisService _service;
        private readonly string _testCsvPath;

        public AnalysisServiceTests()
        {
            _mockLogger = new Mock<ILogger<AnalysisService>>();
            _service = new AnalysisService(_mockLogger.Object);
            _testCsvPath = Path.GetTempFileName();
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithValidData_ReturnsSummary()
        {
            // Arrange
            var csvContent = @"Model,ProductCode,Manufacturer,Price (inc VAT),Rating,ReviewCount,Features,IsEnergyEfficient,Guarantee
Model1,PC1,Samsung,£2999.99,4.5,10,""Feature1,Feature2"",true,5 years
Model2,PC2,Samsung,£3999.99,4.0,15,""Feature1,Feature3"",true,7 years
Model3,PC3,LG,£1999.99,4.8,5,""Feature2,Feature3"",false,3 years";

            File.WriteAllText(_testCsvPath, csvContent);

            try
            {
                // Act
                var summary = await _service.GenerateSummaryAsync(_testCsvPath);

                // Assert
                Assert.NotNull(summary);
                Assert.Equal(3, summary.TotalProducts);
                Assert.Equal(2999.99m, summary.AveragePrice);
                Assert.Equal(4.43, Math.Round(summary.AverageRating, 2));
                Assert.Equal(30, summary.TotalReviews);
                Assert.Equal(2, summary.EnergyEfficientCount);

                Assert.Equal(2, summary.ManufacturerCount["Samsung"]);
                Assert.Equal(1, summary.ManufacturerCount["LG"]);

                Assert.Equal(2, summary.TopFeatures["Feature1"]);
                Assert.Equal(2, summary.TopFeatures["Feature2"]);
                Assert.Equal(2, summary.TopFeatures["Feature3"]);
            }
            finally
            {
                File.Delete(_testCsvPath);
            }
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithEmptyFile_ReturnsEmptySummary()
        {
            // Arrange
            var csvContent = @"Model,ProductCode,Manufacturer,Price (inc VAT),Rating,ReviewCount,Features,IsEnergyEfficient,Guarantee
";
            File.WriteAllText(_testCsvPath, csvContent);

            try
            {
                // Act
                var summary = await _service.GenerateSummaryAsync(_testCsvPath);

                // Assert
                Assert.NotNull(summary);
                Assert.Equal(0, summary.TotalProducts);
                Assert.Equal(0, summary.AveragePrice);
                Assert.Equal(0, summary.AverageRating);
                Assert.Equal(0, summary.TotalReviews);
                Assert.Equal(0, summary.EnergyEfficientCount);
                Assert.Empty(summary.TopFeatures);
                Assert.Empty(summary.ManufacturerCount);
            }
            finally
            {
                File.Delete(_testCsvPath);
            }
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithInvalidFilePath_ThrowsException()
        {
            // Arrange
            var invalidPath = "invalid/path/file.csv";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                _service.GenerateSummaryAsync(invalidPath));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithVariousPriceFormats_CalculatesCorrectAverage()
        {
            // Arrange
            var csvContent = @"Model,ProductCode,Manufacturer,Price (inc VAT),Rating,ReviewCount,Features,IsEnergyEfficient,Guarantee
Model1,PC1,Samsung,£1999.99,4.5,10,""Feature1"",true,5 years
Model2,PC2,Samsung,£2999.99,4.0,15,""Feature2"",true,7 years
Model3,PC3,LG,,4.8,5,""Feature3"",false,3 years";

            File.WriteAllText(_testCsvPath, csvContent);

            try
            {
                // Act
                var summary = await _service.GenerateSummaryAsync(_testCsvPath);

                // Assert
                Assert.NotNull(summary);
                Assert.Equal(2499.99m, summary.AveragePrice); // Average of 1999.99 and 2999.99
            }
            finally
            {
                File.Delete(_testCsvPath);
            }
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithDuplicateFeatures_CountsCorrectly()
        {
            // Arrange
            var csvContent = @"Model,ProductCode,Manufacturer,Price (inc VAT),Rating,ReviewCount,Features,IsEnergyEfficient,Guarantee
Model1,PC1,Samsung,£2999.99,4.5,10,""Feature1,Feature1,Feature2"",true,5 years
Model2,PC2,Samsung,£3999.99,4.0,15,""Feature1,Feature2"",true,7 years";

            File.WriteAllText(_testCsvPath, csvContent);

            try
            {
                // Act
                var summary = await _service.GenerateSummaryAsync(_testCsvPath);

                // Assert
                Assert.NotNull(summary);
                Assert.Equal(3, summary.TopFeatures["Feature1"]); // 2 from first row + 1 from second
                Assert.Equal(2, summary.TopFeatures["Feature2"]); // 1 from each row
            }
            finally
            {
                File.Delete(_testCsvPath);
            }
        }

        [Theory]
        [InlineData("", 0)] // Empty string
        [InlineData("Invalid", 0)] // Invalid format
        [InlineData("£1999.99", 1999.99)] // Standard format
        public async Task GenerateSummaryAsync_WithDifferentPriceFormats_ParsesCorrectly(string priceInput, decimal expectedOutput)
        {
            // Arrange
            var csvContent = $@"Model,ProductCode,Manufacturer,Price (inc VAT),Rating,ReviewCount,Features,IsEnergyEfficient,Guarantee
Model1,PC1,Samsung,{priceInput},4.5,10,""Feature1"",true,5 years";

            File.WriteAllText(_testCsvPath, csvContent);

            try
            {
                // Act
                var summary = await _service.GenerateSummaryAsync(_testCsvPath);

                // Assert
                Assert.NotNull(summary);
                Assert.Equal(expectedOutput, summary.AveragePrice);
            }
            finally
            {
                File.Delete(_testCsvPath);
            }
        }
    }
}

