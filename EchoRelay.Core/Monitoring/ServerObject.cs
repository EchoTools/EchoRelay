using EchoRelay.Core.Game;
using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class ServerObject
{
    public string Ip { get; set; } = "";

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "apiservice_host")]
    public string? ApiServiceHost { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "configservice_host")]
    public string? ConfigServiceHost { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "loginservice_host")]
    public string? LoginServiceHost { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "matchingservice_host")]
    public string? MatchingServiceHost { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "serverdb_host")]
    public string? ServerDbHost { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "transactionservice_host")]
    public string? TransactionServiceHost { get; set; }
    
    public string PublisherLock { get; set; } = "rad15_live";
    public bool Online { get; set; } = false;
}