using EchoRelay.Core.Game;
using EchoRelay.Core.Server;
using System.Text.Json.Serialization;

namespace EchoRelay.API.Public
{
    public class PublicServerInfo
    {
            [JsonPropertyName("serverAddress")] 
            public string ServerAddress { get; set; }
            
            [JsonPropertyName("apiServiceHost")]
            public string? ApiServiceUrl  { get; set; }
            
            [JsonPropertyName("configServiceHost")]
            public string ConfigServiceUrl { get; set; }
            
            [JsonPropertyName("loginServiceHost")]
            public string LoginServiceUrl { get; set; }
            
            [JsonPropertyName("matchingServiceHost")]
            public string MatchingServiceUrl { get; set; }
            
            [JsonPropertyName("serverDbUrl")]
            public string? ServerDbUrl { get; set; }
            
            [JsonPropertyName("transactionServiceHost")]
            public string TransactionServiceUrl { get; set; }
            
            [JsonPropertyName("publisherLock")]
            public string PublisherLock { get; set; }
            
            [JsonPropertyName("isOnline")] 
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
