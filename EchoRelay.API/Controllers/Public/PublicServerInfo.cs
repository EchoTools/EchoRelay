using EchoRelay.Core.Game;
using EchoRelay.Core.Server;
using EchoRelay.Core.Server.Services.ServerDB;
using Newtonsoft.Json;

namespace EchoRelay.API
{
    public class PublicServerInfo
    {
            [JsonProperty("ip")] 
            public string Ip { get; set; }
            
            [JsonProperty("apiservice_host")]
            public string ApiServiceHost { get; set; }
            
            [JsonProperty("configservice_host")]
            public string ConfigServiceHost { get; set; }
            
            [JsonProperty("loginservice_host")]
            public string LoginServiceHost { get; set; }
            
            [JsonProperty("matchingservice_host")]
            public string MatchingServiceHost { get; set; }
            
            [JsonProperty("serverdb_host")]
            public string ServerDbHost { get; set; }
            
            [JsonProperty("transactionservice_host")]
            public string TransactionServiceHost { get; set; }
            
            [JsonProperty("publisher_lock")]
            public string PublisherLock { get; set; }
            
            [JsonProperty("online")] 
            public bool Online { get; set; }
            
            public PublicServerInfo(Server server, ServiceConfig serviceConfig)
            {
                Ip = server.PublicIPAddress?.ToString() ?? "localhost";
                ApiServiceHost = serviceConfig.ApiServiceHost;
                ConfigServiceHost = serviceConfig.ConfigServiceHost;
                LoginServiceHost = serviceConfig.LoginServiceHost;
                MatchingServiceHost = serviceConfig.MatchingServiceHost;
                ServerDbHost = serviceConfig.ServerDBServiceHost;
                TransactionServiceHost = serviceConfig.TransactionServiceHost;
                PublisherLock = serviceConfig.PublisherLock ?? "rad15_live";
                Online = true;
            }
    }
}
