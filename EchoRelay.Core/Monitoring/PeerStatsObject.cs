using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class PeerStatsObject
{
    [JsonProperty(PropertyName = "serverIP")]
    public string? ServerIp { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "gameServers")]
    public int? GameServers { get; set; } = 0;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "login")]
    public int? Login { get; set; } = 0;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "matching")]
    public int? Matching { get; set; } = 0;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "config")]
    public int? Config { get; set; } = 0;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "transaction")]
    public int? Transaction { get; set; } = 0;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "serverdb")]
    public int? ServerDb { get; set; } = 0;
}