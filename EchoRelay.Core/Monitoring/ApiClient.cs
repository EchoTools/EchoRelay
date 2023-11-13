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

    public async Task<string> EncryptAndSendAsync(string endpoint, string data)
    {
        HttpContent content = new StringContent(data, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            throw new Exception($"API request failed with status code: {response.StatusCode}");
    }

    public async Task DeleteAsync(string endpoint)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"DELETE request failed with status code: {response.StatusCode}");
        }
    }
    
    public async Task<string> ReceiveAndDecryptAsync(string endpoint)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        throw new Exception($"API request failed with status code: {response.StatusCode}");
    }
}