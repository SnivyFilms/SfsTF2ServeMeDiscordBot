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
        private readonly string _apiKeyNA;
        private readonly string _apiKeyEU;
        private readonly string _apiKeyAU;
        private readonly string _apiKeySEA;

        public ServemeService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKeyNA = configuration["ServemeApiKeyNA"];
            _apiKeyEU = configuration["ServemeApiKeyEU"];
            _apiKeyAU = configuration["ServemeApiKeyAU"];
            _apiKeySEA = configuration["ServemeApiKeySEA"];
            //_httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        // Existing method for creating reservations
        public async Task<JObject> CreateReservationAsync(string region, string startDate, string startTime, string endDate,
            string endTime, string passwordString, string stvPasswordString, string rconString, string mapString, 
            int serverId, int? serverConfigId, bool enablePlugins, bool enableDemos, bool autoEnd)
        {
            string utcOffset = GetRegionTimeOffset(region);
            var startsAt = $"{startDate}T{startTime}:00.000{utcOffset}";
            var endsAt = $"{endDate}T{endTime}:00.000{utcOffset}";

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
            HttpResponseMessage response;
            switch (region)
            {
                case "NA":
                    response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations?api_key={_apiKeyNA}", requestBody);
                    break;
                case "EU":
                    response = await _httpClient.PostAsJsonAsync($"https://serveme.tf/api/reservations?api_key={_apiKeyNA}", requestBody);
                    break;
                case "AU":
                    response = await _httpClient.PostAsJsonAsync($"https://au.serveme.tf/api/reservations?api_key={_apiKeyNA}", requestBody);
                    break;
                case "SEA":
                    response = await _httpClient.PostAsJsonAsync($"https://sea.serveme.tf/api/reservations?api_key={_apiKeyNA}", requestBody);
                    break;
                default:
                    response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations?api_key={_apiKeyNA}", requestBody);
                    break;
            }
            var content = await response.Content.ReadAsStringAsync();
            JObject reservationResponse;
            try
            {
                reservationResponse = JObject.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }

            return reservationResponse;
        }

        public async Task<JObject> FindServersAsync(string region, string startDate, string startTime, string endDate, string endTime)
        {
            string utcOffset = GetRegionTimeOffset(region);
            var startsAt = $"{startDate}T{startTime}:00.000{utcOffset}";
            var endsAt = $"{endDate}T{endTime}:00.000{utcOffset}";

            var requestBody = new
            {
                reservation = new
                {
                    starts_at = startsAt,
                    ends_at = endsAt,
                }
            };

            HttpResponseMessage response;
            switch (region)
            {
                case "NA":
                    response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations/find_servers?api_key={_apiKeyNA}", requestBody);
                    break;
                case "EU":
                    response = await _httpClient.PostAsJsonAsync($"https://serveme.tf/api/reservations/find_servers?api_key={_apiKeyEU}", requestBody);
                    break;
                case "AU":
                    response = await _httpClient.PostAsJsonAsync($"https://au.serveme.tf/api/reservations/find_servers?api_key={_apiKeyAU}", requestBody);
                    break;
                case "SEA":
                    response = await _httpClient.PostAsJsonAsync($"https://sea.serveme.tf/api/reservations/find_servers?api_key={_apiKeySEA}", requestBody);
                    break;
                default:
                    response = await _httpClient.PostAsJsonAsync($"https://na.serveme.tf/api/reservations/find_servers?api_key={_apiKeyNA}", requestBody);
                    break;
            }

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

        public async Task<JObject> UpdateReservationAsync(string region, int reservationId, int? serverId = null, string? startDate = null, string? startTime = null,
            string? endDate = null, string? endTime = null, string? password = null, string? stvPassword = null, string? map = null,
            int? serverConfigId = null, bool? enablePlugins = null, bool? enableDemos = null, bool? autoEnd = null)
        {
            HttpResponseMessage reservationDetailsResponse;
            switch (region)
            {
                case "NA":
                    reservationDetailsResponse = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyNA}");
                    break;
                case "EU":
                    reservationDetailsResponse = await _httpClient.GetAsync($"https://serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyEU}");
                    break;
                case "AU":
                    reservationDetailsResponse = await _httpClient.GetAsync($"https://au.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyAU}");
                    break;
                case "SEA":
                    reservationDetailsResponse = await _httpClient.GetAsync($"https://sea.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeySEA}");
                    break;
                default:
                    reservationDetailsResponse = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyNA}");
                    break;
            }
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
            {
                string utcOffset = GetRegionTimeOffset(region);
                reservationUpdate.starts_at = $"{startDate}T{startTime}:00.000{utcOffset}";
            }

            if (!string.IsNullOrEmpty(endDate) && !string.IsNullOrEmpty(endTime))
            {
                string utcOffset = GetRegionTimeOffset(region);
                reservationUpdate.ends_at = $"{endDate}T{endTime}:00.000{utcOffset}";
            }

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
            HttpResponseMessage updatedReservationResponse;
            switch (region)
            {
                case "NA":
                    updatedReservationResponse = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyNA}");
                    break;
                case "EU":
                    updatedReservationResponse = await _httpClient.GetAsync($"https://serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyEU}");
                    break;
                case "AU":
                    updatedReservationResponse = await _httpClient.GetAsync($"https://au.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyAU}");
                    break;
                case "SEA":
                    updatedReservationResponse = await _httpClient.GetAsync($"https://sea.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeySEA}");
                    break;
                default:
                    updatedReservationResponse = await _httpClient.GetAsync($"https://na.serveme.tf/api/reservations/{reservationId}?api_key={_apiKeyNA}");
                    break;
            }
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
        private string GetRegionTimeOffset(String region)
        {
            string utcOffset = "";
            switch (region)
            {
                case "NA":
                    utcOffset = "-05:00";
                    break;
                case "EU":
                    utcOffset = "+01:00";
                    break;
                case "AU":
                    utcOffset = "+11:00";
                    break;
                case "SEA":
                    utcOffset = "+08:00";
                    break;
                default:
                    utcOffset = "-05:00";
                    break;
            }
            return utcOffset;
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
