/*
using Discord;
using Discord.Interactions;
using SfsTF2ServeMeBot.Services;

namespace SfsTF2ServeMeBot.Commands;

public class DemosCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DemosService _demosService;

    public DemosCommands(DemosService demosService)
    {
        _demosService = demosService;
    }

    [SlashCommand("get_demos", "Get recent Demos.TF demos by SteamID64")]
    public async Task GetDemos(string steamId, int limit = 5)
    {
        var demos = await _demosService.GetRecentDemosAsync(steamId, limit);

        var embed = new EmbedBuilder().WithTitle($"Recent Demos for {steamId}");
        foreach (var demo in demos)
        {
            embed.AddField("Demo", $"[Link to Demo]({demo["url"]})", true);
        }

        await RespondAsync(embed: embed.Build());
    }
}*/