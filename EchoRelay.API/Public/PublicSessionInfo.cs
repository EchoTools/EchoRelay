using EchoRelay.Core.Server.Messages.ServerDB;
using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API.Public
{
    public class PublicSessionInfo
    {
        [JsonProperty("serverAddress")]
        public string ServerAddress { get; set; }
        
        [JsonProperty("sessionIp")]
        public string SessionIp { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
        
        [JsonProperty("level")]
        public string? Level { get; set; }
        
        [JsonProperty("gameMode")]
        public string? GameMode { get; set; }
        
        [JsonProperty("playerCount")]
        public int PlayerCount { get; set; }
        
        [JsonProperty("sessionId")]
        public string? SessionId { get; set; }

        [JsonProperty("isLocked")]
        public bool IsLocked { get; set; }
        
        [JsonProperty("gameServerId")]
        public ulong GameServerId { get; set; }
        
        [JsonProperty("activePlayerLimit")]
        public int? ActivePlayerLimit { get; set; }

        [JsonProperty("playerLimit")]
        public int PlayerLimit { get; set; }
        
        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }
        
        public PublicSessionInfo(RegisteredGameServer gameServer, bool isPrivate)
        {
            SessionId = isPrivate ? "" : gameServer.SessionId.ToString();
            SessionIp = gameServer.ExternalAddress.ToString();
            GameServerId = gameServer.ServerId;
            ServerAddress = gameServer.Server.PublicIPAddress?.ToString() ?? "localhost";
            Level = gameServer.SessionLevelSymbol == null ? "" : gameServer.Peer.Service.Server.SymbolCache.GetName(gameServer.SessionLevelSymbol.Value);
            GameMode = gameServer.SessionGameTypeSymbol == null ? "" : gameServer.Peer.Server.SymbolCache.GetName(gameServer.SessionGameTypeSymbol.Value);
            PlayerLimit = gameServer.SessionPlayerLimits.TotalPlayerLimit;
            ActivePlayerLimit = gameServer.SessionPlayerLimits.FixedActiveGameParticipantTarget;
            PlayerCount = gameServer.SessionPlayerSessions.Count;
            IsLocked = gameServer.SessionLocked;
            IsPublic = gameServer.SessionLobbyType == ERGameServerStartSession.LobbyType.Public;
            Region = gameServer.Peer.Service.Server.SymbolCache.GetName(gameServer.RegionSymbol) ?? "";
        }
    }
}