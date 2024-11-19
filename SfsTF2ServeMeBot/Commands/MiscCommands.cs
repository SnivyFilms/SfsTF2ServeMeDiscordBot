using Discord;
using Discord.Interactions;
using SfsTF2ServeMeBot.Modules;

namespace SfsTF2ServeMeBot.Commands;

public class MiscCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Get the latency of the bot")]
    public async Task PingAsync()
    {
        await DeferAsync();
        int latency = Context.Client.Latency;
        var embed = new EmbedBuilder()
            .WithTitle("Pong!")
            .AddField("🏓", $"{latency}ms", true)
            .WithFooter(EmbedFooterModule.Footer)
            .WithColor(Color.DarkTeal)
            .Build();
        await FollowupAsync(embed: embed);
    }
    [SlashCommand("help", "When ran, it shares each parameter and how to format them.")]
    public async Task Help()
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Help")
            .AddField("Region", "Determines the region of serveme, NA/EU/AU/SEA. This determines the region timescale offset. NA: -5, EU: +1, AU: -11, SEA: +8")
            .AddField("Start Date", "Provide the date for when the reservation should start. Provided in the format YYYY-MM-DD. Example: 2024-04-09 for April 9th, 2024.", true)
            .AddField("Start Time", "Provide the time for when the reservation should start. Provided in a 24 hour clock style. Example: 21:30 for 9:30 PM.", true)
            .AddField("End Date", "Provide the date for when the reservation should end. Provided in the format YYYY-MM-DD. Example: 2024-06-09 for June 9th, 2024.", true)
            .AddField("End Time", "Provide the time for when the reservation should end. Provided in a 24 hour clock style. Example: 23:30 for 11:30 PM.", true)
            .AddField("Password", "This is the password for the server for both regular and SDR Connects. The entire US Keyboard is supported for inputs.", true)
            .AddField("STV Password", "This is the password for STV. The entire US Keyboard is supported for inputs.", true)
            .AddField("Rcon", "This is the password for remote console. The entire US Keyboard is supported for inputs. This will be sent to the user who runs the command. This CANNOT be changed without reserving a new server.", true)
            .AddField("Map", "This is where the map goes. The full map name is required. Example: cp_snakewater_final1", true)
            .AddField("Server ID", "This is the server id that you can get by running /find_servers. You must use this to get a server, names or server ips will not work.", true)
            .AddField("Starting Config ID", "This is where you define the starting config. For your convenience a list is provided with most RGL Configs, which you can just click.", true)
            .AddField("Enable Plugins", "A true/false option to enable server plugins, such as SOAPs", true)
            .AddField("Enable Demos", "A true/false option to enable auto uploading STV Demo to Demos.tf.", true)
            .AddField("Auto End Enabled", "A true/false option to enable auto ending the reservation if the server is empty.", true)
            .AddField("Reservation ID", "This will be provided to you when you /reserve_server, you will need this if you need to /update_reservation", true)
            .AddField("Command: /find_servers", "Fill out the required fields and it will return a list of available servers and their Server IDs.", true)
            .AddField("Command: /reserve_server", "Fill out the required fields and it will reserve a server. Most info will be publicly displayed, rcon info will be sent to the user who ran the command.", true)
            .AddField("Command: /update_reservation", "Fill out the reservation id and any of the fields to update the reservation.", true)
            .AddField("Command: /help", "Show this help message.", true)
            .AddField("Command: /ping", "Gets the latency of the bot.", true)
            .WithColor(Color.Magenta)
            .WithFooter(EmbedFooterModule.Footer)
            .Build();
        await FollowupAsync(embed: embed);
    }
}