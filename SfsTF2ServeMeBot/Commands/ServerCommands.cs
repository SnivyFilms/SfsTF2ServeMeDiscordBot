using Discord;
using Discord.Interactions;
using Newtonsoft.Json.Linq;
using SfsTF2ServeMeBot.Modules;
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
        [Summary("Region", "Determines which region is used, NA, EU, SEA, AU"),
         Choice("North America", 1),
         Choice("Europe", 2),
         Choice("South East Asia", 3),
         Choice("Australia", 4)]
        int region,
        [Summary("StartDate", "The start date in YYYY-MM-DD. I.E. 2024-04-20 for April 20th, 2024")] string startDate,
        [Summary("StartTime", "The start time in a 24 hour clock format HH:MM. I.E. 21:30 for 9:30 PM.")] string startTime,
        [Summary("EndDate", "The start date in YYYY-MM-DD. I.E. 2024-06-09 for June 9th, 2024")] string endDate,
        [Summary("EndTime", "The start time in a 24 hour clock format HH:MM. I.E. 23:30 for 11:30 PM.")] string endTime,
        [Summary("ServerPassword", "This is the password that people will use to connect to the server.")] string password,
        [Summary("StvPassword", "This is the password that people will use to connect to the STV of server.")] string stvPassword,
        [Summary("RconPassword", "Rcon Password, This cannot be changed after reserving the server. This will be DMed to you.")] string rcon,
        [Summary("Map", "This is the map that the server will start on.")] string map,
        [Summary("ServerId", "This is the ServerId of the server you want to reserve, use /find_server to get the ServerId.")]int serverId,
        [Summary("StartingConfig", "This is the config that the server will start on."),
         Choice("RGL 6s 5CP Improved Timers", 99),
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
        [Summary("EnablePlugins", "Enables/Disables plugins, such as SOAPs.")] bool enablePlugins,
        [Summary("EnableDemos", "Enables/Disables STV demo uploading to demos.tf.")] bool enableDemos,
        [Summary("AutoEnd", "Enables/Disables the server from ending when the server empties out.")] bool autoEnd)
    {
        await DeferAsync();

        try
        {
            var reservationResponse = await _servemeService.CreateReservationAsync(
                region,
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
                enableDemos,
                autoEnd);

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
                .AddField("Auto End Enabled", reservation["auto_end"]?.ToString() ?? "N/A", true)
                .AddField("Selected Config", ConfigNames.ContainsKey(startingConfigId) ? ConfigNames[startingConfigId] : "Unknown Config", true)
                .WithColor(Color.Green)
                .WithFooter(EmbedFooterModule.Footer)
                .Build();

            await FollowupAsync(embed: embed);

            var dmChannel = await Context.User.CreateDMChannelAsync();
            var dmEmbed = new EmbedBuilder()
                .WithTitle("RCON Info")
                .AddField("RCON Address", reservation["rcon_address"]?.ToString() ?? "N/A", true)
                .AddField("RCN Password", reservation["rcn_password"]?.ToString() ?? "N/A", true)
                .WithColor(Color.Green)
                .WithFooter(EmbedFooterModule.Footer)
                .Build();
            await dmChannel.SendMessageAsync(embed: dmEmbed);

        }
        catch (HttpRequestException ex)
        {
            // Handle any errors by informing the user
            var embed = new EmbedBuilder()
                .WithTitle("Server Reservation Failure")
                .AddField("Reason:", "Do you have the correct region selected?", true)
                .AddField("Error Code", ex.Message, true)
                .WithColor(Color.Red)
                .WithFooter(EmbedFooterModule.Footer)
                .Build();
            await FollowupAsync(embed: embed);
            //await FollowupAsync("There was an error reserving the server. Please try again later.");
            //Console.WriteLine($"Error creating reservation: {ex.Message}");
        }
    }

    [SlashCommand("find_servers", "Find available TF2 servers")]
    public async Task FindServers(
        [Summary("Region", "Determines which region is used, NA, EU, SEA, AU"),
         Choice("North America", 1),
         Choice("Europe", 2),
         Choice("South East Asia", 3),
         Choice("Australia", 4)]
        int region,
        [Summary("StartDate", "The start date in YYYY-MM-DD. I.E. 2024-04-20 for April 20th, 2024")] string startDate,
        [Summary("StartTime", "The start time in a 24 hour clock format HH:MM. I.E. 21:30 for 9:30 PM.")] string startTime,
        [Summary("EndDate", "The start date in YYYY-MM-DD. I.E. 2024-06-09 for June 9th, 2024")] string endDate,
        [Summary("EndTime", "The start time in a 24 hour clock format HH:MM. I.E. 23:30 for 11:30 PM.")] string endTime)
    {
        await DeferAsync();

        try
        {
            var availableServers = await _servemeService.FindServersAsync(
                region,
                startDate, 
                startTime, 
                endDate, 
                endTime);
            var servers = availableServers["servers"]?.ToList();
        
            if (servers == null || servers.Count == 0)
            {
                var noServerEmbed = new EmbedBuilder()
                    .WithTitle("No servers found matching the criteria")
                    .AddField("Unavailable Servers", "No servers found matching the criteria", true)
                    .WithColor(Color.Red)
                    .WithFooter(EmbedFooterModule.Footer)
                    .Build();
                await FollowupAsync(embed: noServerEmbed);
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
            var embed = new EmbedBuilder()
                .WithTitle("Server Search Failure")
                .AddField("Reason:", "Do you have the correct region selected?", true)
                .AddField("Error Code", ex.Message, true)
                .WithColor(Color.Red)
                .WithFooter(EmbedFooterModule.Footer)
                .Build();
            await FollowupAsync(embed: embed);
        }
        /*catch (Exception ex)
        {
            await FollowupAsync("An unexpected error occurred.");
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }*/
    }

    [SlashCommand("update_reservation", "Allows you to update a preexisting reservation")]
    public async Task UpdateReservation(
        [Summary("Region", "Determines which region is used, NA, EU, SEA, AU"),
         Choice("North America", 1),
         Choice("Europe", 2),
         Choice("South East Asia", 3),
         Choice("Australia", 4)]
        int region,
        [Summary("ReservationId", "You will need this to change anything with the reservation.")] int reservationId,
        [Summary("ServerId", "This is the ServerId of the server you want to change to, use /find_server to get the ServerId.")]int? serverId = null,
        [Summary("StartDate", "This is where you can change the start date in YYYY-MM-DD.")] string? startDate = null,
        [Summary("StartTime", "This is where you can change the start time in a 24 hour clock format HH:MM")] string? startTime = null,
        [Summary("EndDate", "This is where you can change the start date in YYYY-MM-DD.")] string? endDate = null,
        [Summary("EndTime", "This is where you can change the start time in a 24 hour clock format HH:MM.")] string? endTime = null,
        [Summary("ServerPassword", "This is where you can change the password that people will use to connect to the server.")] string? password = null,
        [Summary("StvPassword", "This is where you can change the password that people will use to connect to the STV of server.")] string? stvPassword = null,
        [Summary("Map", "This is where you can change the map that the server will start on.")] string? map = null,
        [Summary("StartingConfig", "This is where you can change the config that the server will start on."),
         Choice("RGL 6s 5CP Improved Timers", 99),
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
        [Summary("EnablePlugins", "This is where you can Enable/Disable plugins, such as SOAPs.")] bool? enablePlugins = null,
        [Summary("EnableDemos", "This is where you can Enable/Disable STV demo uploading to demos.tf.")] bool? enableDemos = null,
        [Summary("AutoEnd", "This is where you can change the Enable/Disable the server from ending when the server empties out.")] bool? autoEnd = null)
    {
        await DeferAsync(); 
        try
        {
            var updatedReservation = await _servemeService.UpdateReservationAsync(
                region,
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
                enableDemos,
                autoEnd);
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
                .AddField("Auto End Enabled", reservation["auto_end"]?.ToString() ?? "N/A", true)
                .AddField("Selected Config", configName, true)
                .WithColor(Color.Green)
                .WithFooter(EmbedFooterModule.Footer)
                .Build();

            await FollowupAsync(embed: embed);
        }
        catch (HttpRequestException ex)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Server Update Reservation Failure")
                .AddField("Reason:", "Is the correct region selected? Is the fields you wanted to update properly set", true)
                .AddField("Error Code", ex.Message, true)
                .WithColor(Color.Red)
                .WithFooter(EmbedFooterModule.Footer)
                .Build();
            await FollowupAsync(embed: embed);
        }
    }
    private EmbedBuilder BuildServerEmbed(List<JToken> servers, int pageIndex)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Available Servers (Page {pageIndex + 1})")
            .WithFooter(EmbedFooterModule.Footer)
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