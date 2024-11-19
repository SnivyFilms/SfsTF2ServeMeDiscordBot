using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SfsTF2ServeMeBot.Services;

namespace SfsTF2ServeMeBot;

public class Program
{
    public static Version botVersion = new Version(1, 1,0);
    public static async Task Main(string[] args)
    {
        // Build the configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())  // Sets base directory to the project's root
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)  // Load appsettings.json
            //.AddEnvironmentVariables()  // Optional: add environment variables for overriding
            .Build();

        // Set up services and dependency injection
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)  // Add configuration to the services collection
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<ServemeService>()
            .AddSingleton<LogsService>()
            .AddSingleton<DemosService>()
            .AddSingleton<Bot>()
            .AddHttpClient()
            .BuildServiceProvider();

        // Start the bot
        var bot = services.GetRequiredService<Bot>();
        await bot.StartAsync();
    }
}