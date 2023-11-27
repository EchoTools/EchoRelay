using EchoRelay.Core.Server.Messages.ServerDB;
using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API;

public class PublicSessionInfo
{
    [JsonProperty("serverIP")]
    public string ServerIp { get; set; }

    [JsonProperty("region")]
    public string Region { get; set; }
    
    [JsonProperty("level")]
    public string? Level { get; set; }
    
    [JsonProperty("gameMode")]
    public string? GameMode { get; set; }
    
    [JsonProperty("playerCount")]
    public int PlayerCount { get; set; }
    
    [JsonProperty("assigned")]
    public bool Unassigned { get; set; }
    
    [JsonProperty("sessionID")]
    public string? SessionId { get; set; }

    [JsonProperty("locked")]
    public bool Locked { get; set; }
    
    [JsonProperty("gameServerID")]
    public ulong GameServerId { get; set; }
    
    [JsonProperty("activePlayerLimit")]
    public int? ActivePlayerLimit { get; set; }

    [JsonProperty("playerLimit")]
    public int PlayerLimit { get; set; }
    
    [JsonProperty("public")]
    public bool @Public { get; set; }
    
    public PublicSessionInfo(RegisteredGameServer gameServer)
    {
        if (!gameServer.SessionStarted)
        {
            throw new Exception("No session found.");
        }

        GameServerId = gameServer.ServerId;
        ServerIp = gameServer.ExternalAddress.ToString();
        SessionId = gameServer.SessionId.ToString();
        Level = gameServer.SessionLevelSymbol == null ? "" : gameServer.Peer.Server.SymbolCache.GetName(gameServer.SessionLevelSymbol.Value);
        GameMode = gameServer.SessionGameTypeSymbol == null ? "" : gameServer.Peer.Server.SymbolCache.GetName(gameServer.SessionGameTypeSymbol.Value);
        PlayerLimit = gameServer.SessionPlayerLimits.TotalPlayerLimit;
        ActivePlayerLimit = gameServer.SessionPlayerLimits.FixedActiveGameParticipantTarget;
        PlayerCount = gameServer.SessionPlayerSessions.Count;
        Locked = gameServer.SessionLocked;
        @Public = gameServer.SessionLobbyType == ERGameServerStartSession.LobbyType.Public;
        Unassigned = gameServer.SessionLobbyType == ERGameServerStartSession.LobbyType.Unassigned;
        Region = gameServer.Peer.Server.SymbolCache.GetName(gameServer.RegionSymbol) ?? "";
        
    }
}