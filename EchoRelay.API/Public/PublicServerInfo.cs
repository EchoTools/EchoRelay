using EchoRelay.Core.Game;
using EchoRelay.Core.Server;
using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API.Public
{
    public class PublicServerInfo
    {
            [JsonProperty("ip")] 
            public string Ip { get; set; }
            
            [JsonProperty("apiServiceHost")]
            public string? ApiServiceHost { get; set; }
            
            [JsonProperty("configServiceHost")]
            public string ConfigServiceHost { get; set; }
            
            [JsonProperty("loginServiceHost")]
            public string LoginServiceHost { get; set; }
            
            [JsonProperty("matchingServiceHost")]
            public string MatchingServiceHost { get; set; }
            
            [JsonProperty("serverDbHost")]
            public string? ServerDbHost { get; set; }
            
            [JsonProperty("transactionServiceHost")]
            public string TransactionServiceHost { get; set; }
            
            [JsonProperty("publisherLock")]
            public string PublisherLock { get; set; }
            
            [JsonProperty("online")] 
            public bool Online { get; set; }
            
            public PublicServerInfo(Server server, bool online = true)
            {
                Ip = server.PublicIPAddress?.ToString() ?? "localhost";

                ServiceConfig serviceConfig = server.Settings.GenerateServiceConfig(Ip, hideKey:true);
                ApiServiceHost = serviceConfig.ApiServiceHost;
                ConfigServiceHost = serviceConfig.ConfigServiceHost;
                LoginServiceHost = serviceConfig.LoginServiceHost;
                MatchingServiceHost = serviceConfig.MatchingServiceHost;
                ServerDbHost = serviceConfig.ServerDBServiceHost;
                TransactionServiceHost = serviceConfig.TransactionServiceHost;
                PublisherLock = serviceConfig.PublisherLock ?? "rad15_live";
                Online = online;
            }
    }
}
