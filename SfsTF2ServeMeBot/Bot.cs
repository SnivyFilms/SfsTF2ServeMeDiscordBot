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

        await _commands.AddModulesAsync(typeof(Bot).Assembly, _services);

        await Task.Delay(-1); // Keep the bot running
    }

    private async Task OnReadyAsync()
    {
        await SyncCommandsAsync();
    }

    private async Task SyncCommandsAsync()
    {
        Console.WriteLine("Registering commands...");

        try
        {
            await _commands.RegisterCommandsGloballyAsync();
            Console.WriteLine("Commands registered successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to register commands: {ex.Message}");
        }
    }
    
    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}
