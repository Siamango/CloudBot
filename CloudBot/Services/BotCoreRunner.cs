using CloudBot.Services.EventHandlers;
using CloudBot.Settings;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace CloudBot.Services;

public class BotCoreRunner : IHostedService
{
    private readonly IEnumerable<IDiscordClientEventHandler> eventHandlers;
    private readonly IOptionsMonitor<ConnectionSettings> connectionSettings;
    private readonly ILogger logger;
    private readonly DiscordSocketClient client;

    public BotCoreRunner(IEnumerable<IDiscordClientEventHandler> eventHandlers, IOptionsMonitor<ConnectionSettings> connectionSettings, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
        this.eventHandlers = eventHandlers;
        this.connectionSettings = connectionSettings;
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogGatewayIntentWarnings = false,
            MessageCacheSize = 128,
            ConnectionTimeout = 5000,
            HandlerTimeout = null,
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info,
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Discord application token {token}", connectionSettings.CurrentValue.DiscordToken);
        logger.LogInformation("Registering {amount} event handlers", eventHandlers.Count());
        foreach (var handler in eventHandlers)
        {
            handler.RegisterHandlers(client);
        }
        await client.SetGameAsync("⚡ Zapping Citizens ⚡");
        await client.LoginAsync(TokenType.Bot, connectionSettings.CurrentValue.DiscordToken);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.DisposeAsync();
    }
}