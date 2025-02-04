using Discord;
using Discord.Commands;
using Discord.Interactions;
using SfsTF2ServeMeBot.Services;
using Newtonsoft.Json.Linq;
using SfsTF2ServeMeBot.Modules;

namespace SfsTF2ServeMeBot.Commands;

public class LogsCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LogsService _logsService;

    public LogsCommands(LogsService logsService)
    {
        _logsService = logsService;
    }

    [SlashCommand("get_logs", "Get Logs from Logs.TF by SteamID64")]
    public async Task GetLogs(
    [Discord.Interactions.Summary("Title", "The title of the log")] string? matchTitle = null,
    [Discord.Interactions.Summary("Map", "The name of the map")] string? mapName = null,
    [Discord.Interactions.Summary("UploaderSteamID64", "The SteamID64 of the uploader")] string? steamIdUploader = null,
    [Discord.Interactions.Summary("PlayerSteamID64", "SteamID64s of players, add a comma to have multiple")] string? stringIdPlayers = null,
    [Discord.Interactions.Summary("DisplayLimit", "The number of logs to get")] int? logLimit = null,
    [Discord.Interactions.Summary("Offset", "The offset for pagination")] int? logOffset = null
    )
    {
        await DeferAsync();
        try
        {
            if (string.IsNullOrEmpty(matchTitle) && string.IsNullOrEmpty(mapName) && string.IsNullOrEmpty(steamIdUploader) && string.IsNullOrEmpty(stringIdPlayers) && logLimit == null && logOffset == null)
            {
                var embedFailNoParameters = new EmbedBuilder()
                    .WithTitle("No Parameters")
                    .AddField("Error Empty Fields", "You failed to provide any parameters", true)
                    .WithColor(Color.Red)
                    .WithFooter(EmbedFooterModule.Footer)
                    .Build();

                await FollowupAsync(embed: embedFailNoParameters);
                return;
            }

            var logs = await _logsService.GetLogsAsync(matchTitle, mapName, steamIdUploader, stringIdPlayers, logLimit, logOffset);

            if (logs.Count == 0)
            {
                var embedFailNoLogs = new EmbedBuilder()
                    .WithTitle("No Logs")
                    .AddField("No Logs Found", "There were no logs found with the given parameters", true)
                    .WithColor(Color.Red)
                    .WithFooter(EmbedFooterModule.Footer)
                    .Build();

                await FollowupAsync(embed: embedFailNoLogs);
                return;
            }

            var embed = BuildLogsEmbed(logs, 0).Build();

            var components = new ComponentBuilder()
                .WithButton("Previous", "logs_page:0", disabled: true)
                .WithButton("Next", "logs_page:1", disabled: logs.Count <= 25)
                .Build();

            await FollowupAsync(embed: embed, components: components);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private EmbedBuilder BuildLogsEmbed(JArray logs, int pageIndex)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Logs.TF Search Results (Page {pageIndex + 1})")
            .WithFooter(EmbedFooterModule.Footer)
            .WithColor(Color.Blue);

        int start = pageIndex * 25;
        int end = Math.Min(start + 25, logs.Count);

        for (int i = start; i < end; i++)
        {
            var log = logs[i];
            var logId = log["id"]?.ToString();
            var logTitle = log["title"]?.ToString() ?? "No Title";
            embed.AddField(logTitle, $"[Link to Log](https://logs.tf/{logId})", true);
        }

        return embed;
    }
    
    [ComponentInteraction("logs_page:*")]
    public async Task HandlePageChange(string page)
    {
        var parts = page.Split(':');
        int pageIndex = int.Parse(parts[0]);
        var logs = JArray.Parse(parts[1]);
        var embed = BuildLogsEmbed(logs, pageIndex).Build();

        var components = new ComponentBuilder()
            .WithButton("Previous", $"logs_page:{pageIndex - 1}:{logs}", disabled: pageIndex == 0)
            .WithButton("Next", $"logs_page:{pageIndex + 1}:{logs}", disabled: (pageIndex + 1) * 10 >= logs.Count)
            .Build();

        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });
    }
}