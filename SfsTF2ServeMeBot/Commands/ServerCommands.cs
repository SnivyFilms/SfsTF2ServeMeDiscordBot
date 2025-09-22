using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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
         Choice("US EDT (-4) ", 1),
         Choice("US EST / CDT (-5)", 2),
         Choice("US CST / MDT (-6)", 3),
         Choice("US MST / PDT (-7)", 4),
         Choice("US PST / AKDT (-8)", 5),
         Choice("US AKST (-9)", 6),
         Choice("US HST (-10)", 7),
         Choice("Europe (+1)", 8),
         Choice("South East Asia (+11)", 9),
         Choice("Australia (+8)", 10)]
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
        [Summary("StartingConfig", "This is the config that the server will start on, use /config_ids to get the config ids")] int startingConfigId,
        [Summary("EnablePlugins", "Enables/Disables plugins, such as SOAPs.")] bool enablePlugins,
        [Summary("EnableDemos", "Enables/Disables STV demo uploading to demos.tf.")] bool enableDemos,
        [Summary("AutoEnd", "Enables/Disables the server from ending when the server empties out.")] bool autoEnd,
        [Summary("DemoCheck", "If true, Demo Check for RGL games will be enabled, otherwise it will be disabled. (Empty = false)")]bool? demoCheck = null)
    {
        await DeferAsync();

        try
        {
            var startUnix = UnixTimestampModule.ConvertToUnixTimestamp(startDate, startTime, region);
            var endUnix = UnixTimestampModule.ConvertToUnixTimestamp(endDate, endTime, region);

            string discordStartTimestamp = UnixTimestampModule.FormatForDiscord(startUnix);
            string discordEndTimestamp = UnixTimestampModule.FormatForDiscord(endUnix);

            var reservationResponse = await _servemeService.CreateReservationAsync(
                region, startDate, startTime, endDate, endTime, password, 
                stvPassword, rcon, map, serverId, startingConfigId, enablePlugins, enableDemos, autoEnd, demoCheck);

            var reservation = reservationResponse["reservation"];
            var server = reservation["server"];

            var embed = new EmbedBuilder()
                .WithTitle("Server Reservation Successful")
                .AddField("Reservation ID", reservation["id"]?.ToString() ?? "N/A", true)
                .AddField("Start Time", discordStartTimestamp, true)
                .AddField("End Time", discordEndTimestamp, true)
                .AddField("Starting Map", reservation["first_map"]?.ToString() ?? "N/A", true)
                .AddField("Plugins Enabled", reservation["enable_plugins"]?.ToString() ?? "N/A", true)
                .AddField("Enabled Demos.tf", reservation["enable_demos_tf"]?.ToString() ?? "N/A", true)
                .AddField("Auto End Enabled", reservation["auto_end"]?.ToString() ?? "N/A", true)
                .AddField("Demo Check Enabled", reservation["disable_democheck"]?.ToString() == null ? "N/A" : reservation["disable_democheck"]!.Value<bool>() ? "False" : "True", true)
                .AddField("Selected Config", _configNames.ContainsKey(startingConfigId) ? _configNames[startingConfigId] : "Unknown Config", true)
                .AddField("Connect Info", $"```yaml\nconnect {server["ip_and_port"]}; password {reservation["password"]}\n```", false)
                .AddField("STV Connect Info", $"```yaml\nconnect {server["ip"]}:{reservation["tv_port"]}; password {reservation["tv_password"]}\n```", false)
                .AddField("SDR Connect Info", $"```yaml\nconnect {reservation["sdr_ip"]}:{reservation["sdr_port"]}; password {reservation["password"]}\n```", false)
                .AddField("SDR STV Connect Info", $"```yaml\nconnect {reservation["sdr_ip"]}:{reservation["sdr_tv_port"]}; password {reservation["tv_relaypassword"]}\n```", false)
                .WithColor(Color.Green)
                .WithFooter(EmbedFooterModule.Footer)
                .Build();

            await FollowupAsync(embed: embed);

            var dmChannel = await Context.User.CreateDMChannelAsync();
            var dmEmbed = new EmbedBuilder()
                .WithTitle("RCON Info")
                .AddField("RCON Address", server["ip_and_port"]?.ToString() ?? "N/A", true)
                .AddField("RCON Password", reservation["rcon"]?.ToString() ?? "N/A", true)
                .AddField("RCON Command", $"```yaml\nrcon_address {server["ip_and_port"]}; rcon_password {reservation["rcon"]}\n```")
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
                .AddField("Reason:", "Do you have the correct region selected?\nDid you make sure that the start time and end time are in the correct format?\nDid you make the end time end before the start time?", true)
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
        Choice("US EDT (-4) ", 1),
        Choice("US EST / CDT (-5)", 2),
        Choice("US CST / MDT (-6)", 3),
        Choice("US MST / PDT (-7)", 4),
        Choice("US PST / AKDT (-8)", 5),
        Choice("US AKST (-9)", 6),
        Choice("US HST (-10)", 7),
        Choice("Europe (+1)", 8),
        Choice("South East Asia (+11)", 9),
        Choice("Australia (+8)", 10)]
        int region,
        [Summary("StartDate", "The start date in YYYY-MM-DD. I.E. 2024-04-20 for April 20th, 2024")] string startDate,
        [Summary("StartTime", "The start time in a 24-hour clock format HH:MM. I.E. 21:30 for 9:30 PM.")] string startTime,
        [Summary("EndDate", "The end date in YYYY-MM-DD. I.E. 2024-06-09 for June 9th, 2024")] string endDate,
        [Summary("EndTime", "The end time in a 24-hour clock format HH:MM. I.E. 23:30 for 11:30 PM.")] string endTime)
    {
        await DeferAsync();
        try
        {
            var availableServers = await _servemeService.FindServersAsync(region, startDate, startTime, endDate, endTime);
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
        
            var buttons = new ComponentBuilder()
                .WithButton("\u2190", "prev_page", ButtonStyle.Primary, disabled: pageIndex == 0)
                .WithButton("\u2192", "next_page", ButtonStyle.Primary, disabled: (pageIndex + 1) * 24 >= servers.Count);

            var message = await FollowupAsync(embed: embed.Build(), components: buttons.Build());

            async Task HandleInteraction(SocketMessageComponent component)
            {
                if (component.Message.Id != message.Id) return;
                if (component.User.Id != Context.User.Id) return;

                if (component.Data.CustomId == "next_page" && (pageIndex + 1) * 24 < servers.Count)
                {
                    pageIndex++;
                }
                else if (component.Data.CustomId == "prev_page" && pageIndex > 0)
                {
                    pageIndex--;
                }

                var newEmbed = BuildServerEmbed(servers, pageIndex);
                var updatedButtons = new ComponentBuilder()
                    .WithButton("\u2190", "prev_page", ButtonStyle.Primary, disabled: pageIndex == 0)
                    .WithButton("\u2192", "next_page", ButtonStyle.Primary, disabled: (pageIndex + 1) * 24 >= servers.Count);

                await component.UpdateAsync(msg =>
                {
                    msg.Embed = newEmbed.Build();
                    msg.Components = updatedButtons.Build();
                });
            }

            Context.Client.ButtonExecuted += HandleInteraction;

            // Automatically remove event handler after 5 minutes to prevent memory leaks
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(async _ =>
            {
                Context.Client.ButtonExecuted -= HandleInteraction;

                var disabledButtons = new ComponentBuilder()
                    .WithButton("\u2190", "prev_page", ButtonStyle.Primary, disabled: true)
                    .WithButton("\u2192", "next_page", ButtonStyle.Primary, disabled: true);

                await message.ModifyAsync(msg =>
                {
                    msg.Components = disabledButtons.Build();
                });
            });
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
    }

    [SlashCommand("update_reservation", "Allows you to update a preexisting reservation")]
    public async Task UpdateReservation(
        [Summary("Region", "Determines which region is used, NA, EU, SEA, AU"),
         Choice("US EDT (-4) ", 1),
         Choice("US EST / CDT (-5)", 2),
         Choice("US CST / MDT (-6)", 3),
         Choice("US MST / PDT (-7)", 4),
         Choice("US PST / AKDT (-8)", 5),
         Choice("US AKST (-9)", 6),
         Choice("US HST (-10)", 7),
         Choice("Europe (+1)", 8),
         Choice("South East Asia (+11)", 9),
         Choice("Australia (+8)", 10)]
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
        [Summary("StartingConfig", "This is where you can change the config that the server will start on.")] int? startingConfigId = null,
        [Summary("EnablePlugins", "This is where you can Enable/Disable plugins, such as SOAPs.")] bool? enablePlugins = null,
        [Summary("EnableDemos", "This is where you can Enable/Disable STV demo uploading to demos.tf.")] bool? enableDemos = null,
        [Summary("AutoEnd", "This is where you can change the Enable/Disable the server from ending when the server empties out.")] bool? autoEnd = null,
        [Summary("DemoCheck", "If true, Demo Check for RGL games will be enabled, otherwise it will be disabled.")] bool? demoCheck = null)
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
                autoEnd,
                demoCheck);
            var reservation = updatedReservation["reservation"];
            var server = reservation["server"];

            int serverConfigId = reservation["server_config_id"]?.Value<int>() ?? -1;
            string configName = _configNames.ContainsKey(serverConfigId) 
                ? _configNames[serverConfigId] 
                : "Unknown Config";
            var embed = new EmbedBuilder()
                .WithTitle("Server Reservation Updated Successfully")
                .AddField("Reservation ID", reservation["id"]?.ToString() ?? "N/A", true)
                .AddField("Start Time", reservation["starts_at"]?.ToString() ?? "N/A", true)
                .AddField("End Time", reservation["ends_at"]?.ToString() ?? "N/A", true)
                .AddField("Starting Map", reservation["first_map"]?.ToString() ?? "N/A", true)
                .AddField("Plugins Enabled", reservation["enable_plugins"]?.ToString() ?? "N/A", true)
                .AddField("Demos Enabled", reservation["enable_demos_tf"]?.ToString() ?? "N/A", true)
                .AddField("Auto End Enabled", reservation["auto_end"]?.ToString() ?? "N/A", true)
                .AddField("Demo Check Enabled", server["disable_democheck"].ToString() ?? "N/A", true)
                .AddField("Selected Config", configName, true)
                .AddField("Connect Info", $"```yaml\nconnect {server["ip_and_port"]}; password {reservation["password"]}\n```", false)
                .AddField("STV Connect Info", $"```yaml\nconnect {server["ip"]}:{reservation["tv_port"]}; password {reservation["tv_password"]}\n```", false)
                .AddField("SDR Connect Info", $"```yaml\nconnect {reservation["sdr_ip"]}:{reservation["sdr_port"]}; password {reservation["password"]}\n```", false)
                .AddField("SDR STV Connect Info", $"```yaml\nconnect {reservation["sdr_ip"]}:{reservation["sdr_tv_port"]}; password {reservation["tv_relaypassword"]}\n```", false)
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
    
    [SlashCommand("config_ids", "Get all available config IDs")]
    public async Task GetConfigIds()
    {
        await DeferAsync();

        int pageIndex = 0;
        var embed = BuildConfigIdsEmbed(pageIndex);

        var buttons = new ComponentBuilder()
            .WithButton("\u2190", "prev_page", ButtonStyle.Primary, disabled: pageIndex == 0)
            .WithButton("\u2192", "next_page", ButtonStyle.Primary, disabled: (pageIndex + 1) * 24 >= _configNames.Count);

        var message = await FollowupAsync(embed: embed.Build(), components: buttons.Build());

        async Task HandleInteraction(SocketMessageComponent component)
        {
            if (component.Message.Id != message.Id) 
                return;
            if (component.User.Id != Context.User.Id) 
                return;

            if (component.Data.CustomId == "next_page" && (pageIndex + 1) * 24 < _configNames.Count)
            {
                pageIndex++;
            }
            else if (component.Data.CustomId == "prev_page" && pageIndex > 0)
            {
                pageIndex--;
            }

            var newEmbed = BuildConfigIdsEmbed(pageIndex);
            var updatedButtons = new ComponentBuilder()
                .WithButton("\u2190", "prev_page", ButtonStyle.Primary, disabled: pageIndex == 0)
                .WithButton("\u2192", "next_page", ButtonStyle.Primary, disabled: (pageIndex + 1) * 24 >= _configNames.Count);

            await component.UpdateAsync(msg =>
            {
                msg.Embed = newEmbed.Build();
                msg.Components = updatedButtons.Build();
            });
        }

        Context.Client.ButtonExecuted += HandleInteraction;

        _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(async _ =>
        {
            Context.Client.ButtonExecuted -= HandleInteraction;

            var disabledButtons = new ComponentBuilder()
                .WithButton("\u2190", "prev_page", ButtonStyle.Primary, disabled: true)
                .WithButton("\u2192", "next_page", ButtonStyle.Primary, disabled: true);

            await message.ModifyAsync(msg =>
            {
                msg.Components = disabledButtons.Build();
            });
        });
    }
    
    private EmbedBuilder BuildServerEmbed(List<JToken> servers, int pageIndex)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Available Servers (Page {pageIndex + 1})")
            .WithFooter(EmbedFooterModule.Footer)
            .WithColor(Color.Blue);

        int start = pageIndex * 24;
        int end = Math.Min(start + 24, servers.Count);

        for (int i = start; i < end; i++)
        {
            var server = servers[i];
            var serverName = server["name"]?.ToString() ?? "Unknown";
            var serverId = server["id"]?.ToString() ?? "N/A";

            embed.AddField("Server", $"{serverName} (ID: {serverId})", true);
        }

        return embed;
    }
    
    private EmbedBuilder BuildConfigIdsEmbed(int pageIndex)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Available Config IDs")
            .WithColor(Color.Blue)
            .WithFooter(EmbedFooterModule.Footer);

        int start = pageIndex * 24;
        int end = Math.Min(start + 24, _configNames.Count);

        foreach (var config in _configNames.Skip(start).Take(end - start))
        {
            embed.AddField($"ID: {config.Key}", config.Value, true);
        }

        return embed;
    }
    
    private readonly Dictionary<int, string> _configNames = new Dictionary<int, string>
    {
        { 1, "ETF2L" },
        { 2, "ETF2L 6v6" },
        { 3, "ETF2L 9v9" },
        { 4, "ETF2L 6v6 5CP" },
        { 5, "ETF2L 6v6 CTF" },
        { 6, "ETF2L 6v6 Stopwatch" },
        { 7, "ETF2L 9v9 5CP" },
        { 8, "ETF2L 9v9 CTF" },
        { 9, "ETF2L 9v9 KOTH" },
        { 10, "ETF2L 9v9 Stopwatch" },
        { 11, "ETF2L Ultiduo" },
        { 12, "ETF2L BBall" },
        { 31, "TFCL Ulti" },
        { 32, "RGL 7s KOTH BO5" },
        { 33, "RGL 7s KOTH" },
        { 34, "RGL 7s Stopwatch" },
        { 35, "RSP Standard" },
        { 36, "RSP Stopwatch" },
        { 37, "RSP KOTH" },
        { 38, "TFCL 6s KOTH" },
        { 39, "TFCL 6s Standard" },
        { 40, "TFCL 6v6 S3" },
        { 41, "TFCL 9v9 S1" },
        { 42, "TFCL HL KOTH" },
        { 43, "TFCL HL Standard" },
        { 44, "TFCL Ulti" },
        { 45, "TFCL Ultiduo Standard" },
        { 46, "Essentials 5CP" },
        { 47, "GIO 6v6" },
        { 48, "GIO 6v6 KOTH" },
        { 49, "GIO 6v6 Stopwatch" },
        { 50, "GIO 6v6 Medieval CP" },
        { 53, "RGL HL KOTH" },
        { 54, "RGL HL KOTH BO5" },
        { 55, "RGL HL Stopwatch" },
        { 57, "KnightComp" },
        { 58, "KnightComp 5CP" },
        { 59, "KnightComp KOTH" },
        { 60, "RGL MM 5CP" },
        { 61, "RGL MM KOTH" },
        { 63, "RGL MM KOTH BO5" },
        { 64, "RGL MM Stopwatch" },
        { 65, "RGL 6s 5CP Match Half 1" },
        { 66, "RGL 6s 5CP Match Half 2" },
        { 67, "RGL 6s KOTH" },
        { 68, "RGL 6s KOTH BO5" },
        { 69, "RGL 6s 5CP Scrim" },
        { 70, "Scream CPoint" },
        { 71, "Scream KOTH" },
        { 72, "Scream PD" },
        { 73, "Scream PLR" },
        { 74, "Scream Stopwatch" },
        { 76, "Respawn 4s KOTH" },
        { 77, "Respawn 4s Standard" },
        { 78, "Respawn HL KOTH" },
        { 79, "Respawn HL Standard" },
        { 80, "Respawn HL Stopwatch" },
        { 86, "RGL NR6s 5CP Match Half 1" },
        { 87, "RGL NR6s 5CP Match Half 2" },
        { 88, "RGL NR6s 5CP Scrim" },
        { 91, "RGL NR6s KOTH" },
        { 92, "RGL NR6s KOTH BO5" },
        { 93, "RGL NR6s Stopwatch" },
        { 99, "RGL 6s 5CP Improved Timers" },
        { 107, "TFArena 6v6" },
        { 109, "RGL 6s 5CP Match Pro" },
        { 110, "RGL 6s KOTH Pro" },
        { 113, "RGL 6s KOTH Scrim" },
        { 114, "TFArena 6v6 S4" },
        { 115, "TFArena 6v6 S4" },
        { 116, "RGL PT Push" },
        { 117, "RGL UD Ultiduo" },
        { 118, "PT PUG" },
        { 121, "CLTF2 4s 3CP" },
        { 122, "CLTF2 4s KOTH" },
        { 123, "Fireside 6v6 5CP" },
        { 124, "Fireside 6v6 KOTH" },
        { 125, "Fireside 6v6 Unity" },
        { 126, "2v2 MGE" },
        { 127, "PT Global Official" },
        { 128, "PT Global PUG" },
        { 129, "CLTF2 Ultiduo" },
        { 130, "SS 5CP" },
        { 131, "SS KOTH" }
    };
}