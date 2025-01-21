using CsvHelper;
using CsvHelper.Configuration;
using Moq;
using Xunit;
using HeatPumpMonitor.API.Converters;

namespace HeatPumpMonitor.API.Tests.Converters
{
    public class FeaturesConverterTests
    {
        private readonly FeaturesConverter _converter;
        private readonly Mock<IReaderRow> _mockReaderRow;
        private readonly Mock<IWriterRow> _mockWriterRow;

        public FeaturesConverterTests()
        {
            _converter = new FeaturesConverter();
            _mockReaderRow = new Mock<IReaderRow>();
            _mockWriterRow = new Mock<IWriterRow>();
        }

        [Theory]
        [InlineData("Feature1,Feature2", new[] { "Feature1", "Feature2" })]
        [InlineData("Feature1", new[] { "Feature1" })]
        [InlineData(" Feature1 , Feature2 ", new[] { "Feature1", "Feature2" })]
        public void ConvertFromString_WithValidInput_ReturnsList(string input, string[] expected)
        {
            // Act
            var result = _converter.ConvertFromString(input, _mockReaderRow.Object, null) as List<string>;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ConvertFromString_WithEmptyOrNullInput_ReturnsEmptyList(string input)
        {
            // Act
            var result = _converter.ConvertFromString(input, _mockReaderRow.Object, null) as List<string>;

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ConvertFromString_WithMultipleEmptyEntries_SkipsEmptyValues()
        {
            // Arrange
            var input = "Feature1,,Feature2,,,Feature3";

            // Act
            var result = _converter.ConvertFromString(input, _mockReaderRow.Object, null) as List<string>;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new[] { "Feature1", "Feature2", "Feature3" }, result);
        }

        [Fact]
        public void ConvertToString_WithValidList_ReturnsCommaSeparatedString()
        {
            // Arrange
            var features = new List<string> { "Feature1", "Feature2", "Feature3" };

            // Act
            var result = _converter.ConvertToString(features, _mockWriterRow.Object, null);

            // Assert
            Assert.Equal("Feature1,Feature2,Feature3", result);
        }

        [Fact]
        public void ConvertToString_WithEmptyList_ReturnsEmptyString()
        {
            // Arrange
            var features = new List<string>();

            // Act
            var result = _converter.ConvertToString(features, _mockWriterRow.Object, null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertToString_WithNullValue_ReturnsEmptyString()
        {
            // Act
            var result = _converter.ConvertToString(null, _mockWriterRow.Object, null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertToString_WithNonListValue_ReturnsEmptyString()
        {
            // Arrange
            var nonListValue = "Not a list";

            // Act
            var result = _converter.ConvertToString(nonListValue, _mockWriterRow.Object, null);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}

