using EchoRelay.Core.Game;
using EchoRelay.Core.Server;
using Newtonsoft.Json;

namespace EchoRelay.API.Public
{
    public class PublicServerInfo
    {
            [JsonProperty("serverAddress")] 
            public string ServerAddress { get; set; }
            
            [JsonProperty("apiservice_host")] 
            public string? ApiServiceUrl  { get; set; }
            
            [JsonProperty("configservice_host")]
            public string ConfigServiceUrl { get; set; }
            
            [JsonProperty("loginservice_host")]
            public string LoginServiceUrl { get; set; }
            
            [JsonProperty("matchingservice_host")]
            public string MatchingServiceUrl { get; set; }
            
            [JsonProperty("serverdb_host")]
            public string? ServerDbUrl { get; set; }
            
            [JsonProperty("transactionservice_host")]
            public string TransactionServiceUrl { get; set; }
            
            [JsonProperty("publisher_lock")]
            public string PublisherLock { get; set; }
            
            [JsonProperty("isOnline")] public bool IsOnline { get; set; }
            
            public PublicServerInfo(Server server, bool online = true)
            {
                ServerAddress = server.PublicIPAddress?.ToString() ?? "localhost";

                ServiceConfig serviceConfig = server.Settings.GenerateServiceConfig(ServerAddress, hideKey:true);
                ApiServiceUrl = serviceConfig.ApiServiceHost;
                ConfigServiceUrl = serviceConfig.ConfigServiceHost;
                LoginServiceUrl = serviceConfig.LoginServiceHost;
                MatchingServiceUrl = serviceConfig.MatchingServiceHost;
                ServerDbUrl = serviceConfig.ServerDBServiceHost;
                TransactionServiceUrl = serviceConfig.TransactionServiceHost;
                PublisherLock = serviceConfig.PublisherLock ?? "rad15_live";
                IsOnline = online;
            }
    }
}
