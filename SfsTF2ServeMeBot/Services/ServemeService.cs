// Services/ServemeService.cs

using System.Dynamic;
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
            string endTime, string passwordString, string stvPasswordString, string rconString, string mapString, 
            int serverId, int? serverConfigId, bool enablePlugins, bool enableDemos, bool autoEnd)
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
                    tv_password = stvPasswordString,
                    rcon = rconString,
                    first_map = mapString,
                    server_id = serverId,
                    server_config_id = serverConfigId,
                    enable_plugins = enablePlugins,
                    enable_demos_tf = enableDemos,
                    auto_end = autoEnd
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations?api_key={_apiKey}", requestBody);
            var content = await response.Content.ReadAsStringAsync();
            
            //Console.WriteLine($"Raw JSON Response: {content}");  // Log raw response for debugging

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
        }

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

            var content = await response.Content.ReadAsStringAsync();
            JObject availableServers;
            try
            {
                availableServers = JObject.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;  // Rethrow to handle in calling method
            }

            return availableServers;
        }

        public async Task<JObject> UpdateReservationAsync(int reservationId, int? serverId = null, string? startDate = null, string? startTime = null,
            string? endDate = null, string? endTime = null, string? password = null, string? stvPassword = null, string? map = null,
            int? serverConfigId = null, bool? enablePlugins = null, bool? enableDemos = null, bool? autoEnd = null)
        { 
            var reservationDetailsResponse = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/{reservationId}?api_key={_apiKey}");
            if (!reservationDetailsResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve reservation details. Status: {(int)reservationDetailsResponse.StatusCode}");
            }

            var reservationContent = await reservationDetailsResponse.Content.ReadAsStringAsync();
            var reservationJson = JObject.Parse(reservationContent);

            var patchUrl = reservationJson["actions"]?["patch"]?.ToString();
            if (string.IsNullOrEmpty(patchUrl))
            {
                throw new InvalidOperationException("Patch URL not found in reservation details.");
            }
            dynamic reservationUpdate = new ExpandoObject();

            if (serverId.HasValue) reservationUpdate.server_id = serverId;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(startTime))
                reservationUpdate.starts_at = $"{startDate}T{startTime}:00.000-05:00";
            if (!string.IsNullOrEmpty(endDate) && !string.IsNullOrEmpty(endTime))
                reservationUpdate.ends_at = $"{endDate}T{endTime}:00.000-05:00";
            if (!string.IsNullOrEmpty(password)) reservationUpdate.password = password;
            if (!string.IsNullOrEmpty(stvPassword)) reservationUpdate.tv_password = stvPassword;
            if (!string.IsNullOrEmpty(map)) reservationUpdate.first_map = map;
            if (serverConfigId.HasValue) reservationUpdate.server_config_id = serverConfigId;
            if (enablePlugins.HasValue) reservationUpdate.enable_plugins = enablePlugins.Value;
            if (enableDemos.HasValue) reservationUpdate.enable_demos_tf = enableDemos.Value;
            if (autoEnd.HasValue) reservationUpdate.auto_end = autoEnd.Value;

            var requestBody = new { reservation = reservationUpdate };
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
            {
                Content = httpContent
            };

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
    

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to update reservation. Status: {(int)response.StatusCode} - Content: {content}");
            }

            JObject updateResponse;
            try
            {
                updateResponse = JObject.Parse(content);
                Console.WriteLine(updateResponse);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }
    
            var updatedReservationResponse = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/{reservationId}?api_key={_apiKey}");
            if (!updatedReservationResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve updated reservation details. Status: {(int)updatedReservationResponse.StatusCode}");
            }
            var updatedContent = await updatedReservationResponse.Content.ReadAsStringAsync();
            JObject updatedReservation;
            try
            {
                updatedReservation = JObject.Parse(updatedContent);
                Console.WriteLine($"Updated reservation data: {updatedReservation}");
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }

            return updatedReservation;
}

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
