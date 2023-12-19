namespace EchoRelay.API
{
    public class ApiSettings
    {
        public string? ApiKey;
        public string? CentralApiUrl;
        public string? CentralApiKey;
        public ApiSettings(string? apiKey, string? centralApiUrl, string? centralApiKey) {
            ApiKey = apiKey;
            CentralApiKey = centralApiKey;
            CentralApiUrl = centralApiUrl;
        }
    }
}
