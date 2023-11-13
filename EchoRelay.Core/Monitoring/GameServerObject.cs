using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class GameServerObject
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "serverIP")]
    public string? ServerIp { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "region")]
    public string? Region { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "level")]
    public string? Level { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "gameMode")]
    public string? GameMode { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "playerCount")]
    public int PlayerCount { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "assigned")]
    public bool Assigned { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "sessionID")]
    public string? SessionId { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "gameServerID")]
    public ulong GameServerId { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "public")]
    public bool @Public { get; set; }
}