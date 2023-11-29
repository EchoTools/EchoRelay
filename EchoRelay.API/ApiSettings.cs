namespace EchoRelay.API
{
    public class ApiSettings
    {
        public string? ApiKey;
        public string? Advertise;
        public ApiSettings(string? apiKey, string? advertise) {
            ApiKey = apiKey;
            Advertise = advertise;
        }
    }
}
