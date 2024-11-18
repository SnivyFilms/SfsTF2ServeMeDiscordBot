/*using Newtonsoft.Json.Linq;

namespace SfsTF2ServeMeBot.Modules
{

    public class TimeZonePreferenceModule
    {
        private static readonly string JsonPath = "usertimezones.json";
        private static readonly object FileLock = new object(); // Lock object for thread-safety

        // Method to get user preference from JSON
        public static async Task<JObject> GetUserPreferencesAsync()
        {
            try
            {
                lock (FileLock)
                {
                    if (!File.Exists(JsonPath))
                    {
                        File.WriteAllText(JsonPath, "{}");
                    }
                }
                var jsonContent = await File.ReadAllTextAsync(JsonPath);

                return JObject.Parse(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading user preferences: {ex.Message}");
                throw;
            }
        }
        public static async Task SaveUserPreferencesAsync(JObject preferences)
        {
            try
            {
                lock (FileLock)
                {
                    File.WriteAllText(JsonPath, preferences.ToString(Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user preferences: {ex.Message}");
                throw;
            }
        }
    }
}*/