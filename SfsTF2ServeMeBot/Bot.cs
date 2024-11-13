using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace SfsTF2ServeMeBot;

public class Bot
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public Bot(IServiceProvider services, IConfiguration configuration)
    {
        _client = new DiscordSocketClient();
        _commands = new InteractionService(_client);
        _services = services;
        _configuration = configuration;
    }

    public async Task StartAsync()
    {
        _client.Log += LogAsync;
        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += async interaction =>
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _commands.ExecuteCommandAsync(context, _services);
        };

        var token = _configuration["DiscordToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentNullException(nameof(token), "A token cannot be null, empty, or contain only whitespace.");
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1); // Keep the bot running
    }

    private async Task OnReadyAsync()
    {
        await SyncCommandsAsync();
    }

    private async Task SyncCommandsAsync()
    {
        Console.WriteLine("Loading command modules...");
        var modules = await _commands.AddModulesAsync(typeof(Bot).Assembly, _services);

        foreach (var module in modules)
        {
            Console.WriteLine($"Loaded module: {module.Name}");
        }
        
        ulong testGuildId = 335903736142626827;
        await _commands.RegisterCommandsToGuildAsync(testGuildId);
        //await _commands.AddModulesAsync(typeof(Bot).Assembly, _services); // Register all command modules
        //await _commands.RegisterCommandsGloballyAsync(); // Sync commands globally for all servers

        Console.WriteLine("Commands synchronized with Discord.");
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}
