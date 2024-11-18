using System.IO;
using Newtonsoft.Json;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SfsTF2ServeMeBot.Commands
{
    public class TimeZoneCommand : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IConfiguration _configuration;

        public TimeZoneCommand(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [SlashCommand("set-time-zone", "Sets your current time zone")]
        public async Task SetTimeZone(
            [Summary("SetTimeZone", "Sets your current timezone."),
             Choice("Atlantic", "-04:00"),
             Choice("Eastern", "-05:00"),
             Choice("Central", "-06:00"),
             Choice("Mountain", "-07:00"),
             Choice("Pacific", "-08:00"),
             Choice("Alaskan", "-09:00"),
             Choice("Hawaii", "-10:00")]
            string timeZone,
            [Summary("AccountForDaylightSavings", "Accounts for daylight savings.")]
            bool accountForDaylightSavings,
            [Summary("HavePrivateResponse", "Will the response be public or just for you to see")]
            bool hasPrivateResponse)
        {
            await DeferAsync(ephemeral: hasPrivateResponse); // Defer with visibility

            var userId = Context.User.Id.ToString();
            var userPreference = new UserPreference
            {
                UserId = userId,
                TimeZone = timeZone,
                AccountForDaylightSavings = accountForDaylightSavings
            };

            var success = SaveUserPreference(userPreference);

            if (success)
            {
                var message = $"✅ Time zone set to **{timeZone}**.\nDaylight savings: **{(accountForDaylightSavings ? "Enabled" : "Disabled")}**.";
                await RespondAsync(message, ephemeral: hasPrivateResponse);
            }
            else
            {
                await RespondAsync("❌ An error occurred while saving your preferences. Please try again later.", ephemeral: true);
            }
        }

        private bool SaveUserPreference(UserPreference userPreference)
        {
            var filePath = "userPreferences.json";
            Dictionary<string, UserPreference> preferences;

            try
            {
                // Step 1: Load existing preferences
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    preferences = JsonConvert.DeserializeObject<Dictionary<string, UserPreference>>(json)
                                 ?? new Dictionary<string, UserPreference>();
                }
                else
                {
                    preferences = new Dictionary<string, UserPreference>();
                }

                // Step 2: Update the preference for this user
                preferences[userPreference.UserId] = userPreference;

                // Step 3: Save updated preferences back to the file
                File.WriteAllText(filePath, JsonConvert.SerializeObject(preferences, Formatting.Indented));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user preference: {ex.Message}");
                return false;
            }
        }
    }

    public class UserPreference
    {
        public string UserId { get; set; }
        public string TimeZone { get; set; }
        public bool AccountForDaylightSavings { get; set; }
    }
}
