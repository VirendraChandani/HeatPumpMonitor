using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HeatPumpMonitor.API.Interfaces;

namespace HeatPumpMonitor.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool ValidateCredentials(string email, string password)
        {
            try
            {
                var configEmail = _configuration["Auth:Email"];
                var configPassword = _configuration["Auth:Password"];

                if (string.IsNullOrEmpty(configEmail) || string.IsNullOrEmpty(configPassword))
                {
                    _logger.LogError("Authentication credentials not configured");
                    return false;
                }

                return email == configEmail && password == configPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials");
                return false;
            }
        }

        public string GenerateJwtToken(string email)
        {
            try
            {
                var key = _configuration["Jwt:Key"] ??
                    throw new InvalidOperationException("JWT key not configured");

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, email),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token");
                throw;
            }
        }
    }
}
