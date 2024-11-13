// Services/ServemeService.cs

using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SfsTF2ServeMeBot.Models;

namespace SfsTF2ServeMeBot.Services
{
    public class ServemeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ServemeService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["ServemeApiKey"];  // Retrieve the API key
            
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Add the API key to default headers if required
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token token={_apiKey}");
        }

        // Existing method for creating reservations
        public async Task<JObject> CreateReservationAsync(string startDate, string startTime, string endDate,
            string endTime, string passwordString, string rconString, string mapString, int? serverConfigId)
        {
            var startsAt = $"{startDate}T{startTime}:00.000+02:00";  // Ensure the correct timezone format
            var endsAt = $"{endDate}T{endTime}:00.000+02:00";  // Ensure the correct timezone format

            var requestBody = new
            {
                reservation = new
                {
                    starts_at = startsAt,
                    ends_at = endsAt,
                    password = passwordString,
                    rcon = rconString,
                    first_map = mapString,
                    server_config_id = serverConfigId,
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations?api_key={_apiKey}", requestBody);
            response.EnsureSuccessStatusCode();
            var reservationResponse = await response.Content.ReadFromJsonAsync<JObject>();  // Return the response as JObject
            return reservationResponse;
        }


        // Existing method for finding servers
        public async Task<JObject> FindServersAsync(string startDate, string startTime, string endDate, string endTime, string location)
        {
            var startsAt = $"{startDate}T{startTime}:00.000";
            var endsAt = $"{endDate}T{endTime}:00.000";

            var requestBody = new
            {
                reservation = new
                {
                    starts_at = startsAt,
                    ends_at = endsAt,
                    flag = "us"
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations/find_servers?api_key={_apiKey}", requestBody);
            var content = await response.Content.ReadAsStringAsync(); // Read response content
            Console.WriteLine(content); // Debugging the raw response
            response.EnsureSuccessStatusCode();
            var availableServers = await response.Content.ReadFromJsonAsync<JObject>();
            return availableServers;
        }

        // New method to handle the "Test GET Reservation" request
        public async Task<JObject> GetTestReservationAsync()
        {
            var response = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/new?api_key={_apiKey}");
            response.EnsureSuccessStatusCode();
            var prefilledReservation = await response.Content.ReadFromJsonAsync<JObject>();
            return prefilledReservation;
        }
    }
}

/*

        // New method to handle the "Test GET Reservation" request
        public async Task<JObject> GetTestReservationAsync()
        {
            var response = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/new?api_key={_apiKey}");
            var content = await response.Content.ReadAsStringAsync();  // Read response content
            Console.WriteLine(content);  // Debugging the raw response

            var prefilledReservation = JsonConvert.DeserializeObject<ServerInfo>(content);
            //response.EnsureSuccessStatusCode();
            //var prefilledReservation = await response.Content.ReadFromJsonAsync<JObject>();
            return new JObject { prefilledReservation };
        }
    }
}*/
