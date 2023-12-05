namespace EchoRelay.API
{
    public class ApiSettings
    {
        public string? ApiKey;
        public string? NotifyCentralApi;
        public string? CentralApiKey;
        public ApiSettings(string? apiKey, string? notifyCentralApi, string? centralApiKey) {
            ApiKey = apiKey;
            CentralApiKey = centralApiKey;
            NotifyCentralApi = notifyCentralApi;
        }
    }
}
