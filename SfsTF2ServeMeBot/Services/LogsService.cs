using Newtonsoft.Json.Linq;

namespace SfsTF2ServeMeBot.Services;

public class LogsService
{
    private readonly HttpClient _httpClient;

    public LogsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JArray> GetRecentLogsAsync(string steamId, int limit)
    {
        var response = await _httpClient.GetStringAsync($"https://logs.tf/api/v1/log?player={steamId}&limit={limit}");
        return JArray.Parse(response);
    }
}