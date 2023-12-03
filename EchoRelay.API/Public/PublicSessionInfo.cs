using System.Text.Json.Serialization;
using EchoRelay.Core.Server.Messages.ServerDB;
using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API.Public
{
    public class PublicSessionInfo
    {
        [JsonPropertyName("serverAddress")]
        public string ServerAddress { get; set; }
        
        [JsonPropertyName("sessionIp")]
        public string SessionIp { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }
        
        [JsonPropertyName("level")]
        public string? Level { get; set; }
        
        [JsonPropertyName("gameMode")]
        public string? GameMode { get; set; }
        
        [JsonPropertyName("playerCount")]
        public int PlayerCount { get; set; }
        
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; }
        
        [JsonPropertyName("gameServerId")]
        public ulong GameServerId { get; set; }
        
        [JsonPropertyName("activePlayerLimit")]
        public int? ActivePlayerLimit { get; set; }

        [JsonPropertyName("playerLimit")]
        public int PlayerLimit { get; set; }
        
        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; }
        
        public PublicSessionInfo(RegisteredGameServer gameServer)
        {
            SessionId = gameServer.SessionId.ToString();
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