using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class PeerStats
{
    private readonly ApiClient _apiClient;

    public PeerStats(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task EditPeerStats(PeerStatsObject jsonObject, string server)
    {
        // Create a StringContent with the JSON data and set the content type
        string jsonData = JsonConvert.SerializeObject(jsonObject);
        string endpoint = $"updatePeerStats/{server}";

        try
        {
            await _apiClient.PostMonitoringData(endpoint, jsonData);
            Console.WriteLine($"Peer stats successfully edited in monitoring.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing the Peer stats in monitoring: {ex.Message}");
        }
    }
}