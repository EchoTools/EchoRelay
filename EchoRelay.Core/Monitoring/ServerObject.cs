using EchoRelay.Core.Game;
using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class ServerObject
{
    public string ip { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string apiservice_host { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string configservice_host { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string loginservice_host { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string matchingservice_host { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string serverdb_host { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string transactionservice_host { get; set; }
    
    public string publisher_lock { get; set; } = "rad15_live";
    public bool online { get; set; } = false;
}