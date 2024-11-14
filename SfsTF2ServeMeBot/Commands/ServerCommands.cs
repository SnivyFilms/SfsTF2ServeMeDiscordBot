// ServerCommands.cs

using Discord;
using Discord.Interactions;
using Newtonsoft.Json.Linq;
using SfsTF2ServeMeBot.Services;

namespace SfsTF2ServeMeBot.Commands;

public class ServerCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ServemeService _servemeService;

    public ServerCommands(ServemeService servemeService)
    {
        _servemeService = servemeService;
    }

    [SlashCommand("reserve_server", "Reserve a server")]
    public async Task ReserveServer(
    string startDate,
    string startTime,
    string endDate,
    string endTime,
    string password,
    string rcon,
    string map,
    int serverId,
    [Choice("RGL 6s 5CP Improved Timers", 99)]
    [Choice("RGL 6s 5CP Match Half 1", 65)]
    [Choice("RGL 6s 5CP Match Half 2", 66)]
    [Choice("RGL 6s 5CP Match Pro", 109)]
    [Choice("RGL 6s 5CP Scrim", 69)]
    [Choice("RGL 6s KOTH", 67)]
    [Choice("RGL 6s KOTH BO5", 68)]
    [Choice("RGL 6s KOTH Pro", 110)]
    [Choice("RGL 6s KOTH Scrim", 113)]
    [Choice("RGL 7s KOTH", 33)]
    [Choice("RGL 7s KOTH BO5", 32)]
    [Choice("RGL 7s Stopwatch", 34)]
    [Choice("RGL HL KOTH", 53)]
    [Choice("RGL HL KOTH BO5", 54)]
    [Choice("RGL HL Stopwatch", 55)]
    [Choice("RGL NR6s 5CP Match Half 1", 86)]
    [Choice("RGL NR6s 5CP Match Half 2", 87)]
    [Choice("RGL NR6s 5CP Scrim", 88)]
    [Choice("RGL NR6s KOTH", 91)]
    [Choice("RGL NR6s KOTH BO5", 92)]
    [Choice("RGL NR6s Stopwatch", 93)]
    int startingConfigId,
    bool enablePlugins,
    bool enableDemos)
    {
        await DeferAsync();

        try
        {
            // Call the ServemeService to create the reservation
            var reservationResponse = await _servemeService.CreateReservationAsync(
                startDate, startTime, endDate, endTime, password, rcon, map, serverId, startingConfigId, enablePlugins, enableDemos);

            // Extract information from the response
            var reservation = reservationResponse["reservation"];
            var server = reservation["server"];
            var actions = reservationResponse["actions"];

            // Create an embed to show reservation details, excluding RCON and sensitive information
            var embed = new EmbedBuilder()
                .WithTitle("Server Reservation Successful")
                .AddField("Reservation ID", reservation["id"]?.ToString() ?? "N/A", true)
                .AddField("Start Time", reservation["starts_at"]?.ToString() ?? "N/A", true)
                .AddField("End Time", reservation["ends_at"]?.ToString() ?? "N/A", true)
                .AddField("Server IP", server["ip_and_port"]?.ToString() ?? "N/A", true)
                .AddField("SDR IP", reservation["sdr_ip"] + ":" + reservation["sdr_port"]?.ToString() ?? "N/A", true)
                .AddField("Password", reservation["password"]?.ToString() ?? "N/A", true)
                .AddField("STV Password", reservation["tv_password"]?.ToString() ?? "N/A", true)
                .AddField("Starting Map", reservation["first_map"]?.ToString() ?? "N/A", true)
                .AddField("Plugins Enabled", reservation["enable_plugins"]?.ToString() ?? "N/A", true)
                .AddField("Enabled Demos.tf", reservation["enable_demos_tf"]?.ToString() ?? "N/A", true)
                .AddField("Selected Config", ConfigNames.ContainsKey(startingConfigId) ? ConfigNames[startingConfigId] : "Unknown Config", true)
                .WithColor(Color.Green)
                .Build();

            // Send the reservation summary to the channel
            await FollowupAsync(embed: embed);

            // Send the RCON details to the user in a DM
            var dmChannel = await Context.User.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(
                $"**RCON Information**:\nRCON Address: {server["ip_and_port"]}\nRCON Password: {reservation["rcon"]}");

        }
        catch (HttpRequestException ex)
        {
            // Handle any errors by informing the user
            await FollowupAsync("There was an error reserving the server. Please try again later.");
            Console.WriteLine($"Error creating reservation: {ex.Message}");
        }
    }

    [SlashCommand("find_servers", "Find available TF2 servers")]
    public async Task FindServers(
        string startDate, 
        string startTime, 
        string endDate, 
        string endTime)
    {
        await DeferAsync();

        try
        {
            var availableServers = await _servemeService.FindServersAsync(startDate, startTime, endDate, endTime);
            var servers = availableServers["servers"]?.ToList();
        
            if (servers == null || servers.Count == 0)
            {
                await FollowupAsync("No servers found matching the criteria.");
                return;
            }

            int pageIndex = 0;
            var embed = BuildServerEmbed(servers, pageIndex);
            var message = await FollowupAsync(embed: embed.Build());

            // Add reactions for navigation
            await message.AddReactionAsync(new Emoji("⬅️"));
            await message.AddReactionAsync(new Emoji("➡️"));

            // Handle reactions for pagination
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // Auto-cancel after 5 minutes

            // Set up an event handler for reactions
            Context.Client.ReactionAdded += async (cache, _, reaction) =>
            {
                // Ensure this reaction is on our message and from the same user
                if (reaction.MessageId != message.Id || reaction.UserId != Context.User.Id) return;

                // Check if the user clicked the left or right arrow
                if (reaction.Emote.Name == "➡️")
                {
                    // Increment page if not at the last page
                    if ((pageIndex + 1) * 10 < servers.Count)
                    {
                        pageIndex++;
                        var newEmbed = BuildServerEmbed(servers, pageIndex);
                        await message.ModifyAsync(msg => msg.Embed = newEmbed.Build());
                    }
                }
                else if (reaction.Emote.Name == "⬅️")
                {
                    // Decrement page if not at the first page
                    if (pageIndex > 0)
                    {
                        pageIndex--;
                        var newEmbed = BuildServerEmbed(servers, pageIndex);
                        await message.ModifyAsync(msg => msg.Embed = newEmbed.Build());
                    }
                }

                // Remove the user's reaction to keep the UI clean
                var msg = await cache.GetOrDownloadAsync();
                await msg.RemoveReactionAsync(reaction.Emote, Context.User);
            };
        }
        catch (HttpRequestException ex)
        {
            await FollowupAsync("Error finding servers. Try again later.");
            Console.WriteLine($"Error fetching server data: {ex.Message}");
        }
        catch (Exception ex)
        {
            await FollowupAsync("An unexpected error occurred.");
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
    
    private EmbedBuilder BuildServerEmbed(List<JToken> servers, int pageIndex)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Available Servers (Page {pageIndex + 1})")
            .WithColor(Color.Blue);

        int start = pageIndex * 10;
        int end = Math.Min(start + 10, servers.Count);

        for (int i = start; i < end; i++)
        {
            var server = servers[i];
            var serverName = server["name"]?.ToString() ?? "Unknown";
            var serverId = server["id"]?.ToString() ?? "N/A";

            embed.AddField("Server", $"{serverName} (ID: {serverId})", false);
        }

        return embed;
    }
    private readonly Dictionary<int, string> ConfigNames = new Dictionary<int, string>
    {
        { 99, "RGL 6s 5CP Improved Timers" },
        { 65, "RGL 6s 5CP Match Half 1" },
        { 66, "RGL 6s 5CP Match Half 2" },
        { 109, "RGL 6s 5CP Match Pro" },
        { 69, "RGL 6s 5CP Scrim" },
        { 67, "RGL 6s KOTH" },
        { 68, "RGL 6s KOTH BO5" },
        { 110, "RGL 6s KOTH Pro" },
        { 113, "RGL 6s KOTH Scrim" },
        { 33, "RGL 7s KOTH" },
        { 32, "RGL 7s KOTH BO5" },
        { 34, "RGL 7s Stopwatch" },
        { 53, "RGL HL KOTH" },
        { 54, "RGL HL KOTH BO5" },
        { 55, "RGL HL Stopwatch" },
        { 86, "RGL NR6s 5CP Match Half 1" },
        { 87, "RGL NR6s 5CP Match Half 2" },
        { 88, "RGL NR6s 5CP Scrim" },
        { 91, "RGL NR6s KOTH" },
        { 92, "RGL NR6s KOTH BO5" },
        { 93, "RGL NR6s Stopwatch" }
    };
}
/*
    [SlashCommand("test_get_reservation", "Test GET reservation")]
    public async Task TestGetReservation()
    {
        // Acknowledge the interaction
        await DeferAsync();

        try
        {
            // Get prefilled reservation
            var prefilledReservation = await _servemeService.GetTestReservationAsync();

            // Extract relevant details from the prefilled reservation response
            var reservationDetails = prefilledReservation["reservation"];

            var embed = new EmbedBuilder()
                .WithTitle("Prefilled Reservation Details")
                .WithColor(Color.Green)
                .AddField("Reservation ID", reservationDetails["id"]?.ToString() ?? "N/A", true)
                .AddField("Start Time", reservationDetails["starts_at"]?.ToString() ?? "N/A", true)
                .AddField("End Time", reservationDetails["ends_at"]?.ToString() ?? "N/A", true)
                .AddField("Server ID", reservationDetails["server_id"]?.ToString() ?? "N/A", true)
                .AddField("Map", reservationDetails["first_map"]?.ToString() ?? "N/A", true)
                .AddField("RCON Password", reservationDetails["rcon"]?.ToString() ?? "N/A", false)
                .AddField("TV Password", reservationDetails["tv_password"]?.ToString() ?? "N/A", false);

            // Send the response embed
            await FollowupAsync(embed: embed.Build());
        }
        catch (HttpRequestException ex)
        {
            // If there is an error, inform the user
            await FollowupAsync("There was an error retrieving the prefilled reservation. Please try again later.");
            Console.WriteLine($"Error fetching prefilled reservation: {ex.Message}");
        }
    }
}*/
