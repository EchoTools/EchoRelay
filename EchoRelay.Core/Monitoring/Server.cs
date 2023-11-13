using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class Server
{
    private readonly ApiClient _apiClient;

    public Server(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task DeleteServerAsync(string server)
    {
        // Define the endpoint for deleting a game server with the server IP
        string endpoint = $"deleteServer/{server}";

        try
        {
            await _apiClient.DeleteAsync(endpoint);
            Console.WriteLine($"Server {server} deleted successfully from monitoring.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting the server from monitoring: {ex.Message}");
        }
    }

    public async Task AddServerAsync(ServerObject jsonObject)
    {
        // Create a StringContent with the JSON data and set the content type
        string jsonData = JsonConvert.SerializeObject(jsonObject);
        string endpoint = "addServer/";

        try
        {
            await _apiClient.EncryptAndSendAsync(endpoint, jsonData);
            Console.WriteLine($"Server successfully added to monitoring.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding the server to monitoring: {ex.Message}");
        }
    }
    
    public async Task EditServer(ServerObject jsonObject, string server)
    {
        // Create a StringContent with the JSON data and set the content type
        string jsonData = JsonConvert.SerializeObject(jsonObject);
        string endpoint = $"updateServer/{server}";

        try
        {
            await _apiClient.EncryptAndSendAsync(endpoint, jsonData);
            Console.WriteLine($"Game server successfully edited in monitoring.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing the game server in monitoring: {ex.Message}");
        }
    }
    
    public async Task<string> GetServerAsync(string server)
    {
        // Define the endpoint for retrieving a game server with the sessionID
        string endpoint = $"listServers/";

        try
        {
            string decryptedData = await _apiClient.ReceiveAndDecryptAsync(endpoint);
            Console.WriteLine($"Server retrieved successfully from monitoring.");
            return decryptedData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving the server from monitoring: {ex.Message}");
            return null;
        }
    }
}