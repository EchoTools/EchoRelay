using EchoRelay.Core.Server;
using System.Text.Json.Serialization;

namespace EchoRelay.API.Public
{
    
    public class PublicPeerStats
    {
        [JsonPropertyName("serverAddress")]
        public string ServerAddress { get; set; }
    
        [JsonPropertyName("gameServer")]
        public int GameServer { get; set; }
    
        [JsonPropertyName("login")]
        public int Login { get; set; }
    
        [JsonPropertyName("matching")]
        public int Matching { get; set; }
    
        [JsonPropertyName("config")]
        public int Config { get; set; }

        [JsonPropertyName("transaction")]
        public int Transaction { get; set; }

        [JsonPropertyName("serverDb")]
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