using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class PeerStatsObject
{
    public string serverIP { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int gameServers { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int login { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int matching { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int config { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int transaction { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int serverdb { get; set; }
}