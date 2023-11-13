using EchoRelay.Core.Game;

namespace EchoRelay.Core.Monitoring;

public class ServerObject
{
    public string ip { get; set; }
    public string apiservice_host { get; set; }
    public string configservice_host { get; set; }
    public string loginservice_host { get; set; }
    public string matchingservice_host { get; set; }
    public string serverdb_host { get; set; }
    public string transactionservice_host { get; set; }
    public string publisher_lock { get; set; } = "rad15_live";
}