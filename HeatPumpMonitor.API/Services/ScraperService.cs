using CsvHelper;
using HeatPumpMonitor.API.Interfaces;
using HeatPumpMonitor.API.Models;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HeatPumpMonitor.API.Services
{
    public class ScraperService : IScraperService
    {
        private readonly ILogger<ScraperService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _scrapeUrl =  "https://www.screwfix.com/c/heating-plumbing/air-sourced-heat-pumps/cat14210002?brand=samsung";

        public ScraperService(IConfiguration configuration, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<List<HeatPumpProduct>> ScrapeHeatPumpsAsync()
        {
            try
            {
                var url = _scrapeUrl;
                var response = await _httpClient.GetStringAsync(url);
                return ExtractProducts(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping heat pump data");
                throw;
            }
        }

        public async Task SaveToCsvAsync(List<HeatPumpProduct> products)
        {
            try
            {
                string csvPath = _configuration["CsvFilePath"] ?? string.Empty;
                using var writer = new StreamWriter(csvPath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                await csv.WriteRecordsAsync(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV save failed");
                throw;
            }
        }

        private List<HeatPumpProduct> ExtractProducts(string htmlContent)
        {
            var products = new List<HeatPumpProduct>();
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'x1__pJ')]");

            if (productNodes != null)
            {
                foreach (var productNode in productNodes)
                {
                    try
                    {
                        var product = new HeatPumpProduct
                        {
                            Model = ExtractText(productNode, ".//h3//span[1]"),
                            ProductCode = ExtractProductCode(productNode),
                            Manufacturer = "Samsung",
                            Price = ExtractPrice(productNode),
                            Features = ExtractFeatures(productNode),
                            Rating = ExtractRating(productNode),
                            ReviewCount = ExtractReviewCount(productNode),
                            IsEnergyEfficient = IsEnergyEfficient(productNode),
                            Guarantee = ExtractGuarantee(productNode)
                        };

                        products.Add(product);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extracting product data");
                    }
                }
            }

            return products;
        }

        private string ExtractText(HtmlNode node, string xpath)
        {
            var element = node.SelectSingleNode(xpath);
            return element?.InnerText.Trim() ?? string.Empty;
        }

        private string ExtractProductCode(HtmlNode node)
        {
            return ExtractText(node, ".//span[contains(@class, 'I7_YA7')]")?.Trim('(', ')') ?? string.Empty;
        }

        private string ExtractPrice(HtmlNode node)
        {
            var priceText = ExtractText(node, ".//span[contains(@class, '_2_gOH8')]");
            priceText = priceText.Replace("£", "").Replace("Inc Vat", "").Replace(",", "").Trim();
            return priceText;
        }

        private List<string> ExtractFeatures(HtmlNode node)
        {
            var features = new List<string>();
            var featureNodes = node.SelectNodes(".//ul[contains(@class, 'z_Eq10')]//li");

            if (featureNodes != null)
            {
                foreach (var featureNode in featureNodes)
                {
                    features.Add(featureNode.InnerText.Trim());
                }
            }

            return features;
        }

        private double ExtractRating(HtmlNode node)
        {
            var ratingElement = node.SelectSingleNode(".//div[contains(@class, 'vQBT0O')]");
            var ratingText = ratingElement?.GetAttributeValue("title", string.Empty) ?? string.Empty;
            if (string.IsNullOrEmpty(ratingText)) return 0;

            // Extract number from text like "Product rating 3 stars out of 5"
            var match = Regex.Match(ratingText, @"\d+");
            return match.Success ? double.Parse(match.Value) : 0;
        }

        private int ExtractReviewCount(HtmlNode node)
        {
            var reviewText = ExtractText(node, ".//span[contains(@aria-hidden, 'true')]");
            if (string.IsNullOrEmpty(reviewText)) return 0;

            reviewText = reviewText.Trim('(', ')');
            return int.TryParse(reviewText, out int count) ? count : 0;
        }

        private bool IsEnergyEfficient(HtmlNode node)
        {
            return node.SelectNodes(".//div[contains(@class, 'BPu2wi')]") != null;
        }

        private string ExtractGuarantee(HtmlNode node)
        {
            var features = ExtractFeatures(node);
            var guarantee = features.FirstOrDefault(f => f.Contains("Guarantee"));
            return guarantee ?? "Not specified";
        }
    }
}
