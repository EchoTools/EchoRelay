using EchoRelay.Core.Game;
using EchoRelay.Core.Server;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace EchoRelay.API.Public
{
    public class PublicServerInfo
    {
            [JsonPropertyName("serverAddress")] 
            [JsonProperty("serverAddress")] 
            public string ServerAddress { get; set; }
            
            [JsonPropertyName("apiservice_host")]
            [JsonProperty("apiservice_host")]
            public string? ApiServiceUrl  { get; set; }
            
            [JsonPropertyName("configservice_host")]
            [JsonProperty("configservice_host")]

            public string ConfigServiceUrl { get; set; }
            
            [JsonPropertyName("loginservice_host")]
            [JsonProperty("loginservice_host")]

            public string LoginServiceUrl { get; set; }
            
            [JsonPropertyName("matchingservice_host")]
            [JsonProperty("matchingservice_host")]

            public string MatchingServiceUrl { get; set; }
            
            [JsonPropertyName("serverdb_host")]
            [JsonProperty("serverdb_host")]

            public string? ServerDbUrl { get; set; }
            
            [JsonPropertyName("transactionservice_host")]
            [JsonProperty("transactionservice_host")]

            public string TransactionServiceUrl { get; set; }
            
            [JsonPropertyName("publisher_lock")]
            [JsonProperty("publisher_lock")]

            public string PublisherLock { get; set; }
            
            [JsonPropertyName("isOnline")] 
            [JsonProperty("isOnline")]
            public bool IsOnline { get; set; }
            
            public PublicServerInfo(Server server, bool online = true)
            {
                ServerAddress = server.PublicIPAddress?.ToString() ?? "localUrl";

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
