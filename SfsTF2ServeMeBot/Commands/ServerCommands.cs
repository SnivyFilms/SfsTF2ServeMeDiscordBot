// ServerCommands.cs

using Discord;
using Discord.Interactions;
using SfsTF2ServeMeBot.Models;
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
    int serverConfigId)
    {
    
        await DeferAsync();
        try
        {
            // Create the reservation and get the response
            var reservationResponse = await _servemeService.CreateReservationAsync(
                startDate, startTime, endDate, endTime, password, rcon, map, serverConfigId);

            // Extract relevant details from the response
            var reservation = reservationResponse["reservation"];
            var server = reservation["server"];
            var actions = reservationResponse["actions"];

            var embed = new EmbedBuilder()
                .WithTitle("Server Reservation Successful")
                .AddField("Reservation ID", reservation["id"]?.ToString() ?? "N/A", true)
                .AddField("Start Time", reservation["starts_at"]?.ToString() ?? "N/A", true)
                .AddField("End Time", reservation["ends_at"]?.ToString() ?? "N/A", true)
                .AddField("Server Name", server["name"]?.ToString() ?? "N/A", true)
                .AddField("Server IP", server["ip_and_port"]?.ToString() ?? "N/A", true)
                .AddField("Password", reservation["password"]?.ToString() ?? "N/A", false)
                //.AddField("RCON Password", reservation["rcon"]?.ToString() ?? "N/A", false)
                //.AddField("TV Password", reservation["tv_password"]?.ToString() ?? "N/A", false)
                //.AddField("TV Relay Password", reservation["tv_relaypassword"]?.ToString() ?? "N/A", false)
                //.AddField("Reservation Actions", $"[Edit Reservation]({actions["patch"]})\n[Delete Reservation]({actions["delete"]})", false)
                .WithColor(Color.Green)
                .Build();

            // Send the response embed
            await FollowupAsync(embed: embed);

            // Optionally, DM the user with sensitive details
            //var dmChannel = await Context.User.CreateDMChannelAsync();
            //await dmChannel.SendMessageAsync(
            //   $"**RCON Information**:\nRCON Address: {server["ip_and_port"]}\nRCON Password: {reservation["rcon"]}");
        }
        catch (HttpRequestException ex)
        {
            // If there is an error, inform the user
            await FollowupAsync("There was an error reserving the server. Please try again later.");
            Console.WriteLine($"Error fetching server reservation: {ex.Message}");
        }
}

    [SlashCommand("find_servers", "Find available TF2 servers")]
    public async Task FindServers(string startDate, string startTime, string endDate, string endTime,
        [Choice("Chicago", "Chicago")] [Choice("Kansas", "Kansas")] [Choice("Dallas", "Dallas")]
        string location) // Accept location parameter
    {
        // Acknowledge the interaction
        await DeferAsync();

        try
        {
            // Find available servers, passing location to filter by region
            var availableServers =
                await _servemeService.FindServersAsync(startDate, startTime, endDate, endTime, location);

            // Build the embed with the available servers data
            var embed = new EmbedBuilder()
                .WithTitle("Available Servers")
                .WithColor(Color.Blue);

            if (availableServers["servers"] != null)
            {
                foreach (var server in availableServers["servers"])
                {
                    embed.AddField("Server ID", server["id"].ToString(), true);
                    embed.AddField("Server Name", server["name"].ToString(), true);
                    embed.AddField("Location", server["location"]["name"].ToString(), true);
                }
            }
            else
            {
                embed.AddField("No Servers Found", "There are no available servers matching your criteria.", false);
            }

            // Send the response embed
            await FollowupAsync(embed: embed.Build());
        }
        catch (HttpRequestException ex)
        {
            // If there is an error, inform the user
            await FollowupAsync("There was an error finding available servers. Please try again later.");
            Console.WriteLine($"Error fetching server data: {ex.Message}");
        }
    }
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
