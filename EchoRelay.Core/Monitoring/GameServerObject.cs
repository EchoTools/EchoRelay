namespace EchoRelay.Core.Monitoring;

public class GameServerObject
{
    public string serverIP { get; set; }
    public string region { get; set; }
    public string type { get; set; }
    public string gameMode { get; set; }
    public int playerCount { get; set; }
    public string sessionID { get; set; }
    public bool @public { get; set; }
}