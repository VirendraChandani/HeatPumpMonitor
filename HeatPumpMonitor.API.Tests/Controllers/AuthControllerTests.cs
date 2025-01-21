using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using HeatPumpMonitor.API.Controllers;
using HeatPumpMonitor.API.Interfaces;
using HeatPumpMonitor.API.Models;

namespace HeatPumpMonitor.API.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
        }

        [Fact]
        public void Login_WithValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            };
            var expectedToken = "jwt_token_here";

            _mockAuthService.Setup(x => x.ValidateCredentials(request.Email, request.Password))
                .Returns(true);
            _mockAuthService.Setup(x => x.GenerateJwtToken(request.Email))
                .Returns(expectedToken);

            // Act
            var result = _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal(request.Email, response.Email);
            Assert.Equal(expectedToken, response.Token);

            _mockAuthService.Verify(x => x.ValidateCredentials(request.Email, request.Password), Times.Once);
            _mockAuthService.Verify(x => x.GenerateJwtToken(request.Email), Times.Once);
        }

        [Fact]
        public void Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            };

            _mockAuthService.Setup(x => x.ValidateCredentials(request.Email, request.Password))
                .Returns(false);

            // Act
            var result = _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ExceptionResponse>(unauthorizedResult.Value);
            Assert.False(errorResponse.Success);
            Assert.Equal("Invalid credentials", errorResponse.Message);

            _mockAuthService.Verify(x => x.ValidateCredentials(request.Email, request.Password), Times.Once);
            _mockAuthService.Verify(x => x.GenerateJwtToken(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Login_WhenExceptionOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            };

            _mockAuthService.Setup(x => x.ValidateCredentials(request.Email, request.Password))
                .Throws(new Exception("Simulated error"));

            // Act
            var result = _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ExceptionResponse>(statusCodeResult.Value);
            Assert.False(errorResponse.Success);
            Assert.Equal("Login failed", errorResponse.Message);

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
