using Newtonsoft.Json.Linq;

namespace SfsTF2ServeMeBot.Services;

public class DemosService
{
    private readonly HttpClient _httpClient;

    public DemosService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JArray> GetRecentDemosAsync(string steamId, int limit)
    {
        var response = await _httpClient.GetStringAsync($"https://api.demos.tf/demos/{steamId}?limit={limit}");
        return JArray.Parse(response);
    }
}