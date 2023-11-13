using System.Security.Cryptography;

namespace EchoRelay.Core.Monitoring;

public class ApiManager
{

    /// <summary>
    /// Uri to the monitoring API
    /// </summary>
    public string URI { get; } = "http://51.75.140.182:3000/api/";


    /// <summary>
    /// Public key to encrypt data
    /// </summary>
    public ApiClient Monitoring { get; }
    
    public Server Server { get; }
    
    public GameServer GameServer { get; }
    
    private static ApiManager instance;
    
    public ApiManager()
    {
        Monitoring = new ApiClient(URI);
        GameServer = new GameServer(Monitoring);
        Server = new Server(Monitoring);
    }
    
    public static ApiManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ApiManager();
            }
            return instance;
        }
    }

    public static Server ServerEndpoints
    {
        get => Instance.Server;
    }
    
    public static GameServer GameServerEndpoints
    {
        get => Instance.GameServer;
    }
}