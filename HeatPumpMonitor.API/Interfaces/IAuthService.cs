namespace HeatPumpMonitor.API.Interfaces
{
    public interface IAuthService
    {
        string GenerateJwtToken(string username);
        bool ValidateCredentials(string email, string password);
    }
}
