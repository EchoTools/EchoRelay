using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class GameServerObject
{
    public string? ServerIp { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "serverIP")]
    public string? Region { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "region")]
    public string? Level { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "level")]
    public string? GameMode { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "gameMode")]
    public int PlayerCount { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "playerCount")]
    public bool Assigned { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "assigned")]
    public string? SessionId { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "sessionID")]
    public ulong GameServerId { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "gameServerID")]
    public bool @Public { get; set; }
}