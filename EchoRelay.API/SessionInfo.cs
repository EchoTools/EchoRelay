using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API
{
    public class SessionInfo
    {
        [JsonProperty("serverId")]
        public ulong ServerId { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("lobbyType")]
        public int LobbyType { get; set; }

        [JsonProperty("gameType")]
        public long GameType { get; set; }

        [JsonProperty("level")]
        public long? Level { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("activePlayerLimit")]
        public int? ActivePlayerLimit { get; set; }

        [JsonProperty("playerLimit")]
        public int PlayerLimit { get; set; }

        [JsonProperty("locked")]
        public bool Locked { get; set; }

        [JsonProperty("playerSessions")]
        public Dictionary<string, string> PlayerSessions { get; set; }

        [JsonProperty("teamSessions")]
        public Dictionary<string, short> TeamSessions { get; set; }

        public SessionInfo(RegisteredGameServer gameServer)
        {
            if (!gameServer.SessionStarted)
            {
                throw new Exception("No session found.");
            }

            ServerId = gameServer.ServerId;
            SessionId = gameServer.SessionId.ToString();
            LobbyType = (short)gameServer.SessionLobbyType;
            GameType = (long)gameServer.SessionGameTypeSymbol;
            Level = gameServer.SessionLevelSymbol;
            Channel = gameServer.SessionChannel.ToString();
            PlayerLimit = gameServer.SessionPlayerLimits.TotalPlayerLimit;
            ActivePlayerLimit = gameServer.SessionPlayerLimits.FixedActiveGameParticipantTarget;
            Locked = gameServer.SessionLocked;
            PlayerSessions = new Dictionary<string, string>();
            TeamSessions = new Dictionary<string,  short>();

            foreach (var player in gameServer.SessionPlayerSessions)
            {
                PlayerSessions.Add(player.Key.ToString(), player.Value.peer.UserId.ToString());
                TeamSessions.Add(player.Key.ToString(), (short)player.Value.requestedTeam);
            }
        }
    }
}
