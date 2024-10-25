using Microsoft.Extensions.Configuration;

namespace DecriptPfx.Services
{   
    public interface IApiKeyValidatorService
    {
        bool IsValidApiKey(string apiKey);
    }

    public class ApiKeyValidatorService : IApiKeyValidatorService
    {
        private readonly string _validApiKey;
        public ApiKeyValidatorService(IConfiguration configuration)
        {
           _validApiKey = configuration.GetValue<string>("ApiKey") ?? throw new ArgumentNullException(nameof(_validApiKey), "API Key cannot be null"); // Pode vir de configurações
        }       

        public bool IsValidApiKey(string apiKey)
        {
            if(string.IsNullOrWhiteSpace(_validApiKey))
            {
                return false;
            }
                        
            return apiKey == _validApiKey;
        }        
    }
}