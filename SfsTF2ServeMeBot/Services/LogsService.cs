using Newtonsoft.Json.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SfsTF2ServeMeBot.Services;

public class LogsService
{
    private readonly HttpClient _httpClient;
    private readonly string _logsApiKey;

    public LogsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logsApiKey = configuration["LogsApiKey"];
    }

    public async Task<JArray> GetLogsAsync(string? matchTitle, string? mapName, string? steamIdUploader, string? stringIdPlayers, int? logLimit, int? logOffset)
    {
        // Builds the request for the logs.tf API
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(matchTitle))
            queryParams.Add($"title={Uri.EscapeDataString(matchTitle)}");
        if (!string.IsNullOrEmpty(mapName))
            queryParams.Add($"map={Uri.EscapeDataString(mapName)}");
        if (!string.IsNullOrEmpty(steamIdUploader))
            queryParams.Add($"uploader={Uri.EscapeDataString(steamIdUploader)}");
        if (!string.IsNullOrEmpty(stringIdPlayers))
            queryParams.Add($"player={Uri.EscapeDataString(stringIdPlayers)}");
        if (logLimit.HasValue)
            queryParams.Add($"limit={logLimit.Value}");
        if (logOffset.HasValue)
            queryParams.Add($"offset={logOffset.Value}");

        // Returns if there is nothing in the query
        if (queryParams.Count == 0)
            return new JArray();

        // Sends the request
        var queryString = string.Join("&", queryParams);
        var response = await _httpClient.GetStringAsync($"https://logs.tf/api/v1/log?{queryString}");
        //Console.WriteLine(response);
        
        var jsonResponse = JObject.Parse(response);

        // Returns the logs
        if (jsonResponse["logs"] is JArray logsArray)
            return logsArray;
    
        // Fail safe just in case it fails at some point
        return new JArray();
    }
}