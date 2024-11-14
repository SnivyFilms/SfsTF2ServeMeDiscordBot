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
        string stvPassword,
        string rcon,
        string map,
        int serverId,
        [Choice("RGL 6s 5CP Improved Timers", 99),
        Choice("RGL 6s 5CP Match Half 1", 65),
        Choice("RGL 6s 5CP Match Half 2", 66),
        Choice("RGL 6s 5CP Match Pro", 109),
        Choice("RGL 6s 5CP Scrim", 69),
        Choice("RGL 6s KOTH", 67),
        Choice("RGL 6s KOTH BO5", 68),
        Choice("RGL 6s KOTH Pro", 110),
        Choice("RGL 6s KOTH Scrim", 113),
        Choice("RGL 7s KOTH", 33),
        Choice("RGL 7s KOTH BO5", 32),
        Choice("RGL 7s Stopwatch", 34),
        Choice("RGL HL KOTH", 53),
        Choice("RGL HL KOTH BO5", 54),
        Choice("RGL HL Stopwatch", 55),
        Choice("RGL NR6s 5CP Match Half 1", 86),
        Choice("RGL NR6s 5CP Match Half 2", 87),
        Choice("RGL NR6s 5CP Scrim", 88),
        Choice("RGL NR6s KOTH", 91),
        Choice("RGL NR6s KOTH BO5", 92),
        Choice("RGL NR6s Stopwatch", 93)]
        int startingConfigId,
        bool enablePlugins,
        bool enableDemos)
    {
        await DeferAsync();

        try
        {
            var reservationResponse = await _servemeService.CreateReservationAsync(
                startDate, 
                startTime, 
                endDate, 
                endTime, 
                password, 
                stvPassword, 
                rcon, 
                map, 
                serverId, 
                startingConfigId, 
                enablePlugins, 
                enableDemos);

            var reservation = reservationResponse["reservation"];
            var server = reservation["server"];

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

            await FollowupAsync(embed: embed);

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
            var availableServers = await _servemeService.FindServersAsync(
                startDate, 
                startTime, 
                endDate, 
                endTime);
            var servers = availableServers["servers"]?.ToList();
        
            if (servers == null || servers.Count == 0)
            {
                await FollowupAsync("No servers found matching the criteria.");
                return;
            }

            int pageIndex = 0;
            var embed = BuildServerEmbed(servers, pageIndex);
            var message = await FollowupAsync(embed: embed.Build());

            await message.AddReactionAsync(new Emoji("⬅️"));
            await message.AddReactionAsync(new Emoji("➡️"));

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            // Set up an event handler for reactions
            Context.Client.ReactionAdded += async (cache, _, reaction) =>
            {
                if (reaction.MessageId != message.Id || reaction.UserId != Context.User.Id) return;

                if (reaction.Emote.Name == "➡️")
                {
                    if ((pageIndex + 1) * 10 < servers.Count)
                    {
                        pageIndex++;
                        var newEmbed = BuildServerEmbed(servers, pageIndex);
                        await message.ModifyAsync(msg => msg.Embed = newEmbed.Build());
                    }
                }
                else if (reaction.Emote.Name == "⬅️")
                {
                    if (pageIndex > 0)
                    {
                        pageIndex--;
                        var newEmbed = BuildServerEmbed(servers, pageIndex);
                        await message.ModifyAsync(msg => msg.Embed = newEmbed.Build());
                    }
                }
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

    [SlashCommand("update_reservation", "Allows you to update a preexisting reservation")]
    public async Task UpdateReservation(
        int reservationId,
        int? serverId = null,
        string? startDate = null,
        string? startTime = null,
        string? endDate = null,
        string? endTime = null,
        string? password = null,
        string? stvPassword = null,
        string? map = null,
        [Choice("RGL 6s 5CP Improved Timers", 99),
        Choice("RGL 6s 5CP Match Half 1", 65),
        Choice("RGL 6s 5CP Match Half 2", 66),
        Choice("RGL 6s 5CP Match Pro", 109),
        Choice("RGL 6s 5CP Scrim", 69),
        Choice("RGL 6s KOTH", 67),
        Choice("RGL 6s KOTH BO5", 68),
        Choice("RGL 6s KOTH Pro", 110),
        Choice("RGL 6s KOTH Scrim", 113),
        Choice("RGL 7s KOTH", 33),
        Choice("RGL 7s KOTH BO5", 32),
        Choice("RGL 7s Stopwatch", 34),
        Choice("RGL HL KOTH", 53),
        Choice("RGL HL KOTH BO5", 54),
        Choice("RGL HL Stopwatch", 55),
        Choice("RGL NR6s 5CP Match Half 1", 86),
        Choice("RGL NR6s 5CP Match Half 2", 87),
        Choice("RGL NR6s 5CP Scrim", 88),
        Choice("RGL NR6s KOTH", 91),
        Choice("RGL NR6s KOTH BO5", 92),
        Choice("RGL NR6s Stopwatch", 93)]
        int? startingConfigId = null,
        bool? enablePlugins = null,
        bool? enableDemos = null)
    {
        await DeferAsync(); 
        try
        {
            var updatedReservation = await _servemeService.UpdateReservationAsync(
                reservationId, 
                serverId,
                startDate, 
                startTime, 
                endDate, 
                endTime, 
                password, 
                stvPassword, 
                map, 
                startingConfigId,
                enablePlugins,
                enableDemos);
            var reservation = updatedReservation["reservation"];
            var server = reservation["server"];

            int serverConfigId = reservation["server_config_id"]?.Value<int>() ?? -1;
            string configName = ConfigNames.ContainsKey(serverConfigId) 
                ? ConfigNames[serverConfigId] 
                : "Unknown Config";
            var embed = new EmbedBuilder()
                .WithTitle("Server Reservation Updated Successfully")
                .AddField("Reservation ID", reservation["id"]?.ToString() ?? "N/A", true)
                .AddField("Start Time", reservation["starts_at"]?.ToString() ?? "N/A", true)
                .AddField("End Time", reservation["ends_at"]?.ToString() ?? "N/A", true)
                .AddField("Server IP", server["ip_and_port"]?.ToString() ?? "N/A", true)
                .AddField("SDR IP", $"{reservation["sdr_ip"]}:{reservation["sdr_port"]}" ?? "N/A", true)
                .AddField("Password", reservation["password"]?.ToString() ?? "N/A", true)
                .AddField("STV Password", reservation["tv_password"]?.ToString() ?? "N/A", true)
                .AddField("Starting Map", reservation["first_map"]?.ToString() ?? "N/A", true)
                .AddField("Plugins Enabled", reservation["enable_plugins"]?.ToString() ?? "N/A", true)
                .AddField("Demos Enabled", reservation["enable_demos_tf"]?.ToString() ?? "N/A", true)
                .AddField("Selected Config", configName, true)
                .WithColor(Color.Green)
                .Build();

            await FollowupAsync(embed: embed);
        }
        catch (HttpRequestException ex)
        {
            // Notify the user of the error
            await FollowupAsync("There was an error updating the server reservation. Please try again later.");
            Console.WriteLine($"Error updating reservation: {ex.Message}");
        }
    }

    [SlashCommand("help", "When ran, it shares each parameter and how to format them.")]
    public async Task Help()
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Help")
            .AddField("Start Date", "Provide the date for when the reservation should start. Provided in the format YYYY-MM-DD. Example: 2024-04-09 for April 9th, 2024.", true)
            .AddField("Start Time", "Provide the time for when the reservation should start. Provided in a 24 hour clock style. Example: 21:30 for 9:30 PM. In US Eastern", true)
            .AddField("End Date", "Provide the date for when the reservation should end. Provided in the format YYYY-MM-DD. Example: 2024-06-09 for June 9th, 2024.", true)
            .AddField("End Time", "Provide the time for when the reservation should end. Provided in a 24 hour clock style. Example: 23:30 for 11:30 PM. In US Eastern", true)
            .AddField("Password", "This is the password for the server for both regular and SDR Connects. The entire US Keyboard is supported for inputs.", true)
            .AddField("STV Password", "This is the password for STV. The entire US Keyboard is supported for inputs.", true)
            .AddField("Rcon", "This is the password for remote console. The entire US Keyboard is supported for inputs. This will be sent to the user who runs the command. This CANNOT be changed without reserving a new server.", true)
            .AddField("Map", "This is where the map goes. The full map name is required. Example: cp_snakewater_final1", true)
            .AddField("Server ID", "This is the server id that you can get by running /find_servers. You must use this to get a server, names or server ips will not work.", true)
            .AddField("Starting Config ID", "This is where you define the starting config. For your convenience a list is provided with most RGL Configs, which you can just click.", true)
            .AddField("Enable Plugins", "A true/false option to enable server plugins, such as SOAPs", true)
            .AddField("Enable Demos", "A true/false option to enable auto uploading STV Demo to Demos.tf.", true)
            .AddField("Reservation ID", "This will be provided to you when you /reserve_server, you will need this if you need to /update_reservation", true)
            .AddField("Command: /find_servers", "Fill out the required fields and it will return a list of available servers and their Server IDs.", true)
            .AddField("Command: /reserve_server", "Fill out the required fields and it will reserve a server. Most info will be publicly displayed, rcon info will be sent to the user who ran the command.", true)
            .AddField("Command: /update_reservation", "Fill out the reservation id and any of the fields to update the reservation.", true)
            .AddField("Command: /help", "Show this help message.", true)
            .WithColor(Color.Magenta)
            .Build();
        await FollowupAsync(embed: embed);
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
