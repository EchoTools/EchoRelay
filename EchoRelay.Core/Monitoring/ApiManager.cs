using System.Security.Cryptography;

namespace EchoRelay.Core.Monitoring;

public class ApiManager
{

    /// <summary>
    /// Uri to the monitoring API
    /// </summary>
    public string URI { get; } = "http://localhost:3000/api/";
    //public string URI { get; } = "http://51.75.140.182:3000/api/";

    public PeerStatsObject peerStatsObject;
    /// <summary>
    /// Public key to encrypt data
    /// </summary>
    public ApiClient Monitoring { get; }
    
    public Server Server { get; }
    
    public GameServer GameServer { get; }
    public PeerStats PeerStats { get; }

    private static ApiManager instance = new ApiManager();
    
    private ApiManager()
    {
        Monitoring = new ApiClient(URI);
        GameServer = new GameServer(Monitoring);
        PeerStats = new PeerStats(Monitoring);
        Server = new Server(Monitoring);
        peerStatsObject = new PeerStatsObject();
    }
    
    // Method to get the singleton instance
    public static ApiManager Instance
    {
        get { return instance; }
    }
}