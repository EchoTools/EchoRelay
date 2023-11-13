using System.Security.Cryptography;
using System.Text;

namespace EchoRelay.Core.Monitoring;
    
//TO DO : Encrypt data
public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    //TODO encrypt data to avoid people to send fake data
    public async Task<string> PostMonitoringData(string endpoint, string data)
    {
        HttpContent content = new StringContent(data, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            throw new Exception($"API request failed with status code: {response.StatusCode}");
    }

    public async Task DeleteMonitoringData(string endpoint)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"DELETE request failed with status code: {response.StatusCode}");
        }
    }

}