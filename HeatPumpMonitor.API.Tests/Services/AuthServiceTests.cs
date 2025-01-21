using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;
using HeatPumpMonitor.API.Services;

namespace HeatPumpMonitor.API.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;
        private readonly Mock<IConfigurationSection> _mockAuthSection;
        private readonly Mock<IConfigurationSection> _mockJwtSection;

        public AuthServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockAuthSection = new Mock<IConfigurationSection>();
            _mockJwtSection = new Mock<IConfigurationSection>();

            _authService = new AuthService(_mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public void ValidateCredentials_WithValidCredentials_ReturnsTrue()
        {
            // Arrange
            var testEmail = "test@example.com";
            var testPassword = "password123";

            _mockConfiguration.Setup(x => x["Auth:Email"]).Returns(testEmail);
            _mockConfiguration.Setup(x => x["Auth:Password"]).Returns(testPassword);

            // Act
            var result = _authService.ValidateCredentials(testEmail, testPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateCredentials_WithInvalidCredentials_ReturnsFalse()
        {
            // Arrange
            var configEmail = "test@example.com";
            var configPassword = "password123";
            var wrongEmail = "wrong@example.com";
            var wrongPassword = "wrongpassword";

            _mockConfiguration.Setup(x => x["Auth:Email"]).Returns(configEmail);
            _mockConfiguration.Setup(x => x["Auth:Password"]).Returns(configPassword);

            // Act & Assert
            Assert.False(_authService.ValidateCredentials(wrongEmail, configPassword));
            Assert.False(_authService.ValidateCredentials(configEmail, wrongPassword));
            Assert.False(_authService.ValidateCredentials(wrongEmail, wrongPassword));
        }

        [Fact]
        public void ValidateCredentials_WithMissingConfiguration_ReturnsFalse()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["Auth:Email"]).Returns((string)null);
            _mockConfiguration.Setup(x => x["Auth:Password"]).Returns((string)null);

            // Act
            var result = _authService.ValidateCredentials("test@example.com", "password123");

            // Assert
            Assert.False(result);
            VerifyLogError("Authentication credentials not configured");
        }

        [Fact]
        public void ValidateCredentials_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["Auth:Email"]).Throws(new Exception("Configuration error"));

            // Act
            var result = _authService.ValidateCredentials("test@example.com", "password123");

            // Assert
            Assert.False(result);
            VerifyLogError("Error validating credentials");
        }

        [Fact]
        public void GenerateJwtToken_WithValidConfiguration_ReturnsToken()
        {
            // Arrange
            var testEmail = "test@example.com";
            var testKey = "your-256-bit-secret-your-256-bit-secret"; // At least 256 bits
            var testIssuer = "test-issuer";

            _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns(testKey);
            _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns(testIssuer);

            // Act
            var token = _authService.GenerateJwtToken(testEmail);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            // Validate token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal(testEmail, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal(testEmail, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.NotNull(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
            Assert.Equal(testIssuer, jwtToken.Issuer);
        }

        [Fact]
        public void GenerateJwtToken_WithMissingJwtKey_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns((string)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _authService.GenerateJwtToken("test@example.com"));
            Assert.Equal("JWT key not configured", exception.Message);
        }

        [Fact]
        public void GenerateJwtToken_WhenExceptionOccurs_LogsAndRethrows()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["Jwt:Key"]).Throws(new Exception("Configuration error"));

            // Act & Assert
            var exception = Assert.Throws<Exception>(() =>
                _authService.GenerateJwtToken("test@example.com"));
            VerifyLogError("Error generating JWT token");
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
