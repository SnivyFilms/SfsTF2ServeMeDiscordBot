﻿using System.Dynamic;
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
        public async Task<JObject> CreateReservationAsync(int region, string startDate, string startTime, string endDate,
            string endTime, string passwordString, string stvPasswordString, string rconString, string mapString, 
            int serverId, int? serverConfigId, bool enablePlugins, bool enableDemos, bool autoEnd)
        {
            var startsAt = $"{startDate}T{startTime}:00.000{GetRegionTimeOffset(region)}";
            var endsAt = $"{endDate}T{endTime}:00.000{GetRegionTimeOffset(region)}";

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
            var response = await _httpClient.PostAsJsonAsync(
                $"https://{GetRegionUrlPrefix(region)}serveme.tf/api/reservations?api_key={GetApiKeyToUse(region)}", requestBody);
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

        public async Task<JObject> FindServersAsync(int region, string startDate, string startTime, string endDate, string endTime)
        {
            var startsAt = $"{startDate}T{startTime}:00.000{GetRegionTimeOffset(region)}";
            var endsAt = $"{endDate}T{endTime}:00.000{GetRegionTimeOffset(region)}";

            var requestBody = new
            {
                reservation = new
                {
                    starts_at = startsAt,
                    ends_at = endsAt
                }
            };
            var response = await _httpClient.PostAsJsonAsync(
                $"https://{GetRegionUrlPrefix(region)}serveme.tf/api/reservations/find_servers?api_key={GetApiKeyToUse(region)}", requestBody);
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {content}");

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


        public async Task<JObject> UpdateReservationAsync(int region, int reservationId, int? serverId = null, string? startDate = null, string? startTime = null,
            string? endDate = null, string? endTime = null, string? password = null, string? stvPassword = null, string? map = null,
            int? serverConfigId = null, bool? enablePlugins = null, bool? enableDemos = null, bool? autoEnd = null)
        {
            var reservationDetailsResponse = 
                await _httpClient.GetAsync(
                    $"https://{GetRegionUrlPrefix(region)}serveme.tf/api/reservations/{reservationId}?api_key={GetApiKeyToUse(region)}");
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
                Console.WriteLine($"Updated reservation data: {updatedReservation}");
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }

            return updatedReservation;
        }
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
