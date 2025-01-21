using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using HtmlAgilityPack;
using Xunit;
using HeatPumpMonitor.API.Services;
using HeatPumpMonitor.API.Models;
using System.Text;

namespace HeatPumpMonitor.API.Tests.Services
{
    public class ScraperServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ScraperService>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly ScraperService _scraperService;

        private const string SampleHtml = @"
            <div class='x1__pJ'>
                <h3><span>Model XYZ123</span></h3>
                <span class='I7_YA7'>(ABC123)</span>
                <span class='_2_gOH8'>£2,999.99 Inc Vat</span>
                <div title='Product rating 4 stars out of 5' class='vQBT0O' />
                <span aria-hidden='true'>(15)</span>
                <ul class='z_Eq10'>
                    <li>Feature 1</li>
                    <li>Feature 2</li>
                    <li>10 Year Guarantee</li>
                </ul>
                <div class='BPu2wi'></div>
            </div>";

        public ScraperServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<ScraperService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            // Setup mock HTTP client
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(SampleHtml)
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(client);

            _scraperService = new ScraperService(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object);
        }

        [Fact]
        public async Task ScrapeHeatPumpsAsync_Success_ReturnsProducts()
        {
            // Act
            var products = await _scraperService.ScrapeHeatPumpsAsync();

            // Assert
            Assert.NotNull(products);
            Assert.Single(products);

            var product = products[0];
            Assert.Equal("Model XYZ123", product.Model);
            Assert.Equal("ABC123", product.ProductCode);
            Assert.Equal("Samsung", product.Manufacturer);
            Assert.Equal("2999.99", product.Price);
            Assert.Equal(4, product.Rating);
            Assert.Equal(15, product.ReviewCount);
            Assert.Equal(3, product.Features.Count);
            Assert.True(product.IsEnergyEfficient);
            Assert.Contains("10 Year Guarantee", product.Guarantee);
        }

        [Fact]
        public async Task ScrapeHeatPumpsAsync_WhenHttpRequestFails_ThrowsException()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(client);

            var service = new ScraperService(
                _mockConfiguration.Object,
                _mockLogger.Object,
                mockFactory.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.ScrapeHeatPumpsAsync());

            VerifyLogError("Error scraping heat pump data");
        }

        [Fact]
        public async Task ScrapeHeatPumpsAsync_WithInvalidHtml_ReturnsEmptyList()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("<div>Invalid HTML</div>")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(client);

            var service = new ScraperService(
                _mockConfiguration.Object,
                _mockLogger.Object,
                mockFactory.Object);

            // Act
            var products = await service.ScrapeHeatPumpsAsync();

            // Assert
            Assert.Empty(products);
        }

        [Fact]
        public async Task SaveToCsvAsync_Success_SavesFile()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            _mockConfiguration.Setup(x => x["CsvFilePath"]).Returns(tempFile);

            var products = new List<HeatPumpProduct>
            {
                new HeatPumpProduct
                {
                    Model = "Test Model",
                    ProductCode = "TEST123",
                    Manufacturer = "Samsung",
                    Price = "2,999.99",
                    Rating = 4.5,
                    ReviewCount = 10,
                    Features = new List<string> { "Feature 1", "Feature 2" },
                    IsEnergyEfficient = true,
                    Guarantee = "5 years"
                }
            };

            try
            {
                // Act
                await _scraperService.SaveToCsvAsync(products);

                // Assert
                Assert.True(File.Exists(tempFile));
                var fileContent = await File.ReadAllTextAsync(tempFile);
                Assert.Contains("Test Model", fileContent);
                Assert.Contains("TEST123", fileContent);
                Assert.Contains("Samsung", fileContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task SaveToCsvAsync_WithInvalidPath_ThrowsException()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["CsvFilePath"]).Returns("Z:\\invalid\\path\\file.csv");

            var products = new List<HeatPumpProduct>
            {
                new HeatPumpProduct { Model = "Test Model" }
            };

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                _scraperService.SaveToCsvAsync(products));

            VerifyLogError("CSV save failed");
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
