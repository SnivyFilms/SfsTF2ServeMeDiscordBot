using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SfsTF2ServeMeBot.Services;

namespace SfsTF2ServeMeBot;

public class BotStarter
{
    public static Version BotVersion = new Version(1, 3,0);
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<ServemeService>()
            .AddSingleton<LogsService>()
            //.AddSingleton<DemosService>()
            .AddSingleton<Bot>()
            .AddHttpClient()
            .BuildServiceProvider();

        // Start the bot
        var bot = services.GetRequiredService<Bot>();
        await bot.StartAsync();
    }
}