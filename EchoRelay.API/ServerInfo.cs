using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API
{
    public class ServerInfo
    {
        [JsonProperty("serverId")]
        public ulong ServerId { get; set; }

        [JsonProperty("internalAddress")]
        public string InternalAddress { get; set; }

        [JsonProperty("externalAddress")]
        public string ExternalAddress { get; set; }

        [JsonProperty("port")]
        public ushort Port { get; set; }

        [JsonProperty("regionSymbol")]
        public long RegionSymbol { get; set; }

        [JsonProperty("versionLock")]
        public long VersionLock { get; set; }

        [JsonProperty("sessionId")]
        public string? SessionId { get; set; }

        public ServerInfo(RegisteredGameServer gameServer)
        {
            ServerId = gameServer.ServerId;
            InternalAddress = gameServer.InternalAddress.ToString();
            ExternalAddress = gameServer.ExternalAddress.ToString();
            Port = gameServer.Port;
            RegionSymbol = gameServer.RegionSymbol;
            VersionLock = gameServer.VersionLock;
            SessionId = gameServer.SessionId.ToString();
        }
    }
}
