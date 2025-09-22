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
        
        // Create Reservation Command Handle
        public async Task<JObject> CreateReservationAsync(int region, string startDate, string startTime, string endDate,
        string endTime, string passwordString, string stvPasswordString, string rconString, string mapString,
        int serverId, int? serverConfigId, bool enablePlugins, bool enableDemos, bool autoEnd, bool? disableDemoCheck = null)
        {
            // Gets start time and the regional time differences for the reservation, combines date and time into one
            var startsAt = $"{startDate}T{startTime}:00.000{GetRegionTimeOffset(region)}";
            var endsAt = $"{endDate}T{endTime}:00.000{GetRegionTimeOffset(region)}";

            dynamic reservationData = new ExpandoObject();
            reservationData.starts_at = startsAt;
            reservationData.ends_at = endsAt;
            reservationData.password = passwordString;
            reservationData.tv_password = stvPasswordString;
            reservationData.rcon = rconString;
            reservationData.first_map = mapString;
            reservationData.server_id = serverId;
            reservationData.server_config_id = serverConfigId;
            reservationData.enable_plugins = enablePlugins;
            reservationData.enable_demos_tf = enableDemos;
            reservationData.auto_end = autoEnd;
            if (disableDemoCheck.HasValue) reservationData.disable_democheck = !disableDemoCheck.Value;
            var requestBody = new
            {
                reservation = reservationData
            };

            // Sends api request
            var response = await _httpClient.PostAsJsonAsync(
                $"https://{GetRegionUrlPrefix(region)}serveme.tf/api/reservations?api_key={GetApiKeyToUse(region)}", requestBody);
            var content = await response.Content.ReadAsStringAsync();
            JObject reservationResponse;
            // Parses the response into a JObject to be read by the discord bot
            try
            {
                reservationResponse = JObject.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }
            // Returns the response to the bot
            return reservationResponse;
        }

        //Finds available servers for a reservation
        public async Task<JObject> FindServersAsync(int region, string startDate, string startTime, string endDate, string endTime)
        {
            // Gets date and time with the regional time differences
            var startsAt = $"{startDate}T{startTime}:00.000{GetRegionTimeOffset(region)}";
            var endsAt = $"{endDate}T{endTime}:00.000{GetRegionTimeOffset(region)}";
            
            // Builds the find server request
            var requestBody = new
            {
                reservation = new
                {
                    starts_at = startsAt,
                    ends_at = endsAt
                }
            };
            // Sends the find server request
            var response = await _httpClient.PostAsJsonAsync(
                $"https://{GetRegionUrlPrefix(region)}serveme.tf/api/reservations/find_servers?api_key={GetApiKeyToUse(region)}", requestBody);
            var content = await response.Content.ReadAsStringAsync();
            // Parses the response into a JObject to be read by the discord bot
            JObject availableServers;
            try
            {
                availableServers = JObject.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }

            return availableServers;
        }

        // Allow to update a preexisting reservation
        public async Task<JObject> UpdateReservationAsync(int region, int reservationId, int? serverId = null, string? startDate = null, string? startTime = null,
            string? endDate = null, string? endTime = null, string? password = null, string? stvPassword = null, string? map = null,
            int? serverConfigId = null, bool? enablePlugins = null, bool? enableDemos = null, bool? autoEnd = null, bool? demoCheck = false)
        {
            // Gets the reservation details
            var reservationDetailsResponse = 
                await _httpClient.GetAsync(
                    $"https://{GetRegionUrlPrefix(region)}serveme.tf/api/reservations/{reservationId}?api_key={GetApiKeyToUse(region)}");
            if (!reservationDetailsResponse.IsSuccessStatusCode)
            {
                // Throws a failure message
                throw new HttpRequestException($"Failed to retrieve reservation details. Status: {(int)reservationDetailsResponse.StatusCode}");
            }
            
            // Gets the response from the API
            var reservationContent = await reservationDetailsResponse.Content.ReadAsStringAsync();
            var reservationJson = JObject.Parse(reservationContent);

            var patchUrl = reservationJson["actions"]?["patch"]?.ToString();
            if (string.IsNullOrEmpty(patchUrl))
            {
                throw new InvalidOperationException("Patch URL not found in reservation details.");
            }
            dynamic reservationUpdate = new ExpandoObject();

            if (serverId.HasValue) reservationUpdate.server_id = serverId;
            
            // Checks if the start date and time are not null, if not run the start time adjustment
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(startTime))
            {
                string utcOffset = GetRegionTimeOffset(region);
                reservationUpdate.starts_at = $"{startDate}T{startTime}:00.000{utcOffset}";
            }
            
            // Checks if the end date and time are not null, if not run the end time adjustment
            if (!string.IsNullOrEmpty(endDate) && !string.IsNullOrEmpty(endTime))
            {
                string utcOffset = GetRegionTimeOffset(region);
                reservationUpdate.ends_at = $"{endDate}T{endTime}:00.000{utcOffset}";
            }

            // Checks if all other parameters, such as password, stv password, map, etc are not null, and if so, adjust it
            if (!string.IsNullOrEmpty(password)) reservationUpdate.password = password;
            if (!string.IsNullOrEmpty(stvPassword)) reservationUpdate.tv_password = stvPassword;
            if (!string.IsNullOrEmpty(map)) reservationUpdate.first_map = map;
            if (serverConfigId.HasValue) reservationUpdate.server_config_id = serverConfigId;
            if (enablePlugins.HasValue) reservationUpdate.enable_plugins = enablePlugins.Value;
            if (enableDemos.HasValue) reservationUpdate.enable_demos_tf = enableDemos.Value;
            if (autoEnd.HasValue) reservationUpdate.auto_end = autoEnd.Value;
            if (demoCheck.HasValue) reservationUpdate.disable_democheck = !demoCheck.Value;

            // Builds and send the request to change a reservation
            var requestBody = new { reservation = reservationUpdate };
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
            {
                Content = httpContent
            };
            
            // Gets the response from the API to update the reservation
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
            
            // Gets the reservation info to send back to the discord bot
            var updatedReservationResponse = 
                await _httpClient.GetAsync(
                    $"https://{GetRegionUrlPrefix(region)}serveme.tf/api/reservations/{reservationId}?api_key={GetApiKeyToUse(region)}");
            if (!updatedReservationResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve updated reservation details. Status: {(int)updatedReservationResponse.StatusCode}");
            }
            var updatedContent = await updatedReservationResponse.Content.ReadAsStringAsync();
            JObject updatedReservation;
            try
            {
                updatedReservation = JObject.Parse(updatedContent);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }

            return updatedReservation;
        }
        
        // Region Time Offset is to cover timezones of users that are likely to use this discord bot.
        // I.E. all US Timezones, Europe, Australia, and South East Asia
        private string GetRegionTimeOffset(int region)
        {
            string utcOffset = "-05:00";
            switch (region)
            {
                case 1:
                    utcOffset = "-04:00";
                    break;
                case 2:
                    utcOffset = "-05:00";
                    break;
                case 3:
                    utcOffset = "-06:00";
                    break;
                case 4:
                    utcOffset = "-07:00";
                    break;
                case 5:
                    utcOffset = "-08:00";
                    break;
                case 6:
                    utcOffset = "-09:00";
                    break;
                case 7:
                    utcOffset = "-10:00";
                    break;
                case 8:
                    utcOffset = "+01:00";
                    break;
                case 9:
                    utcOffset = "+11:00";
                    break;
                case 10:
                    utcOffset = "+08:00";
                    break;
            }
            return utcOffset;
        }

        // Uses the regional time offset to determine which region should be used
        private string GetRegionUrlPrefix(int region)
        {
            string UrlPrefix = "na.";
            switch (region)
            {
                case 1 or 2 or 3 or 4 or 5 or 6 or 7:
                    UrlPrefix = "na.";
                    break;
                case 8:
                    UrlPrefix = "";
                    break;
                case 9:
                    UrlPrefix = "au.";
                    break;
                case 10:
                    UrlPrefix = "sea.";
                    break;
                default:
                    UrlPrefix = "na.";
                    break;
            }
            return UrlPrefix;
        }
        
        // Uses the regional time offset to determine which API key should be used
        private string GetApiKeyToUse(int region)
        {
            string ApiKey = _apiKeyNA;
            switch (region)
            {
                case 1 or 2 or 3 or 4 or 5 or 6 or 7:
                    ApiKey = _apiKeyNA;
                    break;
                case 8:
                    ApiKey = _apiKeyEU;
                    break;
                case 9:
                    ApiKey = _apiKeyAU;
                    break;
                case 10:
                    ApiKey = _apiKeySEA;
                    break;
                default:
                    ApiKey = _apiKeyNA;
                    break;
            }
            return ApiKey;
        }
    }
}
