namespace EchoRelay.API
{
    public class ApiSettings
    {
        public string[] ApiKeys { get; }

        public ApiSettings(string[] apiKeys)
        {
            ApiKeys = apiKeys;
        }
    }
}
