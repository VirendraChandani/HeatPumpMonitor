using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace HeatPumpMonitor.API.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email Required")]
        [DefaultValue("admin@viren.com")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password Required")]
        [DefaultValue("Admin123!")]
        public string Password { get; set; } = string.Empty;
    }
}
