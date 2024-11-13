// Commands/LogsCommands.cs

using Discord;
using Discord.Interactions;
using SfsTF2ServeMeBot.Services;

namespace SfsTF2ServeMeBot.Commands;

public class LogsCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LogsService _logsService;

    public LogsCommands(LogsService logsService)
    {
        _logsService = logsService;
    }

    [SlashCommand("get_logs", "Get recent Logs.TF logs by SteamID64")]
    public async Task GetLogs(string steamId, int limit = 5)
    {
        var logs = await _logsService.GetRecentLogsAsync(steamId, limit);

        var embed = new EmbedBuilder().WithTitle($"Recent Logs for {steamId}");
        foreach (var log in logs)
        {
            embed.AddField("Log", $"[Link to Log](https://logs.tf/{log["id"]})", true);
        }

        await RespondAsync(embed: embed.Build());
    }
}