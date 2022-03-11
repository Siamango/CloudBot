﻿using BetterHaveIt.Repositories;
using CloudBot.EventHandlers;
using Discord;
using Discord.WebSocket;

namespace CloudBot.Services;

public class BotCoreRunner : ICoreRunner
{
    private readonly IEnumerable<IDiscordClientEventHandler> eventHandlers;
    private readonly IConfiguration configuration;
    private readonly ILogger logger;
    private readonly DiscordSocketClient client;

    public BotCoreRunner(IEnumerable<IDiscordClientEventHandler> eventHandlers, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
        this.eventHandlers = eventHandlers;
        this.configuration = configuration;

        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogGatewayIntentWarnings = false,
            MessageCacheSize = 128,
            ConnectionTimeout = 5000,
            HandlerTimeout = null,
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info,
        });
        logger.LogInformation("Discord application token {token}", configuration.GetValue<string>("Connection:DiscordToken"));
        logger.LogInformation("Registering {amount} event handlers", eventHandlers.Count());
        foreach (var handler in eventHandlers)
        {
            handler.RegisterHandlers(client);
        }
        client.SetGameAsync("⚡ Zapping Citizens ⚡");
    }

    public async Task RunAsync()
    {
        logger.LogInformation("Environment {environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        await client.LoginAsync(TokenType.Bot, configuration.GetValue<string>("Connection:DiscordToken"));
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }
}