using EchoRelay.Core.Server;
using Newtonsoft.Json;

namespace EchoRelay.API.Public
{
    
    public class PublicPeerStats
    {
        [JsonProperty("serverAddress")]
        public string ServerAddress { get; set; }
    
        [JsonProperty("gameServer")]
        public int GameServer { get; set; }
    
        [JsonProperty("login")]
        public int Login { get; set; }
    
        [JsonProperty("matching")]
        public int Matching { get; set; }
    
        [JsonProperty("config")]
        public int Config { get; set; }

        [JsonProperty("transaction")]
        public int Transaction { get; set; }

        [JsonProperty("serverDb")]
        public int ServerDb { get; set; }
    
        public PublicPeerStats(Server server)
        {
            ServerAddress = server.PublicIPAddress?.ToString() ?? "localhost";
            GameServer = server.ServerDBService.Registry.RegisteredGameServers.Count;
            Login = server.LoginService.Peers.Count;
            Matching = server.MatchingService.Peers.Count;
            Config = server.ConfigService.Peers.Count;
            Transaction = server.TransactionService.Peers.Count;
            ServerDb = server.ServerDBService.Peers.Count;
        
        }
    }
}