namespace DecriptPfx.EndpointValidation
{
    public interface IApiKeyValidation
    {
        bool IsValidApiKey(string userApiKey);
    }
}