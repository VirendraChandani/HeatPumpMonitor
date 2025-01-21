using Microsoft.AspNetCore.Mvc;
using HeatPumpMonitor.API.Interfaces;
using HeatPumpMonitor.API.Models;

namespace HeatPumpMonitor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!_authService.ValidateCredentials(request.Email, request.Password))
                {
                    return Unauthorized(new ExceptionResponse { Success = false, Message = "Invalid credentials" });
                }

                var token = _authService.GenerateJwtToken(request.Email);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Email = request.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new ExceptionResponse { Success = false, Message = "Login failed" });
            }
        }
    }
}
