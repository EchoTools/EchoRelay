namespace EchoRelay.API
{
    public class ApiSettings
    {
        public string? ApiKey;
        public string? Advertise;
        public string? CentralApiKey;
        public ApiSettings(string? apiKey, string? advertise, string? centralApiKey) {
            ApiKey = apiKey;
            CentralApiKey = centralApiKey;
            Advertise = advertise;
        }
    }
}
