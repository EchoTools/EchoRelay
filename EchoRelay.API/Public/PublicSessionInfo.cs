using System.Text.Json;
using EchoRelay.Core.Server.Messages.ServerDB;
using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API.Public
{
    public class PublicSessionInfo
    {
        [JsonProperty("serverIp")]
        public string ServerIp { get; set; }
        
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

        [JsonProperty("locked")]
        public bool Locked { get; set; }
        
        [JsonProperty("gameServerId")]
        public ulong GameServerId { get; set; }
        
        [JsonProperty("activePlayerLimit")]
        public int? ActivePlayerLimit { get; set; }

        [JsonProperty("playerLimit")]
        public int PlayerLimit { get; set; }
        
        [JsonProperty("public")]
        public bool @Public { get; set; }
        
        public PublicSessionInfo(RegisteredGameServer gameServer)
        {
            SessionId = gameServer.SessionId.ToString();
            SessionIp = gameServer.ExternalAddress.ToString();
            GameServerId = gameServer.ServerId;
            ServerIp = gameServer.ExternalAddress.ToString();
            Level = gameServer.SessionLevelSymbol == null ? "" : gameServer.Peer.Service.Server.SymbolCache.GetName(gameServer.SessionLevelSymbol.Value);
            GameMode = gameServer.SessionGameTypeSymbol == null ? "" : gameServer.Peer.Server.SymbolCache.GetName(gameServer.SessionGameTypeSymbol.Value);
            PlayerLimit = gameServer.SessionPlayerLimits.TotalPlayerLimit;
            ActivePlayerLimit = gameServer.SessionPlayerLimits.FixedActiveGameParticipantTarget;
            PlayerCount = gameServer.SessionPlayerSessions.Count;
            Locked = gameServer.SessionLocked;
            @Public = gameServer.SessionLobbyType == ERGameServerStartSession.LobbyType.Public;
            Region = gameServer.Peer.Service.Server.SymbolCache.GetName(gameServer.RegionSymbol) ?? "";
        }
    }
}