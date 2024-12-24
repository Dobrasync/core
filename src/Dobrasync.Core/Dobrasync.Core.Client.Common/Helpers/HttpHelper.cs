using Newtonsoft.Json;

namespace Dobrasync.Core.Client.Common.Helpers;

public class HttpHelper
{
    private readonly HttpClient _httpClient;

    public HttpHelper()
    {
        _httpClient = new HttpClient();
    }

    public async Task<T> PostAsync<T>(string url, HttpContent content)
    {
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(jsonString);
    }
}