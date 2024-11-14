// Services/ServemeService.cs

using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            string endTime, string passwordString, string rconString, string mapString, int serverId, int? serverConfigId, bool enablePlugins, bool enableDemos)
        {
            var startsAt = $"{startDate}T{startTime}:00.000-05:00";
            var endsAt = $"{endDate}T{endTime}:00.000-05:00";

            var requestBody = new
            {
                reservation = new
                {
                    starts_at = startsAt,
                    ends_at = endsAt,
                    password = passwordString,
                    rcon = rconString,
                    first_map = mapString,
                    server_id = serverId,
                    server_config_id = serverConfigId,
                    enable_plugins = enablePlugins,
                    enable_demos_tf = enableDemos
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations?api_key={_apiKey}", requestBody);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Raw JSON Response: {content}");  // Log raw response for debugging

            // Step 2: Attempt to parse the JSON manually if necessary
            JObject reservationResponse;
            try
            {
                reservationResponse = JObject.Parse(content); // Attempt to parse into JObject
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;  // Rethrow to handle in calling method
            }

            return reservationResponse;
            //var reservationResponse = await response.Content.ReadFromJsonAsync<JObject>();  // Return the response as JObject
            //return reservationResponse;
        }


        // Existing method for finding servers
        public async Task<JObject> FindServersAsync(string startDate, string startTime, string endDate, string endTime)
        {
            var startsAt = $"{startDate}T{startTime}:00.000-05:00";
            var endsAt = $"{endDate}T{endTime}:00.000-05:00";

            var requestBody = new
            {
                reservation = new
                {
                    starts_at = startsAt,
                    ends_at = endsAt,
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations/find_servers?api_key={_apiKey}", requestBody);

            // Step 1: Read the response as a string and log it for inspection
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw JSON Response: {content}");  // Log raw response for debugging

            // Step 2: Attempt to parse the JSON manually if necessary
            JObject availableServers;
            try
            {
                availableServers = JObject.Parse(content); // Attempt to parse into JObject
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;  // Rethrow to handle in calling method
            }

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
