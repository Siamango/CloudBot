using CloudBot.Handlers;
using CloudBot.CommandsModules;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudBot.Services;

public class BotCoreRunner : ICoreRunner
{
    private readonly IServiceProvider services;
    private readonly IConfiguration configuration;
    private readonly ILogger<BotCoreRunner> logger;
    private readonly CommandService commandsService;
    private readonly DiscordSocketClient client;

    public BotCoreRunner(IServiceProvider services, IConfiguration configuration, ILogger<BotCoreRunner> logger)
    {
        this.services = services;
        this.configuration = configuration;
        this.logger = logger;

        logger.LogInformation("Token: {token}", configuration.GetValue<string>("Connection:DiscordToken"));
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogLevel = LogSeverity.Info,
        });

        var commandService = services.GetService<CommandService>();
        if (commandService == null)
        {
            throw new Exception("Null command service");
        }
        commandsService = commandService;
        commandsService.Log += Log;
        client.Log += Log;
        client.MessageReceived += HandleMessageAsync;
    }

    public async Task RunAsync()
    {
        await ConfigureCommandsModules();
        logger.LogInformation("Environment {environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        await client.LoginAsync(TokenType.Bot, configuration.GetValue<string>("Connection:DiscordToken"));
        await client.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }

    private async Task ConfigureCommandsModules()
    {
        await commandsService.AddModuleAsync<EndpointModule>(services);
        await commandsService.AddModuleAsync<AdminCommandsModule>(services);
        await commandsService.AddModuleAsync<CommandsModule>(services);
    }

    private async Task HandleMessageAsync(SocketMessage arg)
    {
        if (arg is not SocketUserMessage msg) return;

        if (msg.Author.Id == client.CurrentUser.Id || msg.Author.IsBot) return;

        var messageHandler = services.GetService<IMessageDispatchHandler>();
        if (messageHandler is not null)
        {
            var res = await messageHandler.Handle(arg);
            if (!res) return;
        }
        int pos = 0;
        if (msg.HasStringPrefix("cb.", ref pos))
        {
            var context = new SocketCommandContext(client, msg);
            var result = await commandsService.ExecuteAsync(context, pos, services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                await msg.Channel.SendMessageAsync(result.ErrorReason);
        }
    }

    private Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                logger.LogCritical($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
                break;

            case LogSeverity.Error:
                logger.LogError($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
                break;

            case LogSeverity.Warning:
                logger.LogWarning($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
                break;

            case LogSeverity.Info:
                logger.LogInformation($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
                break;

            case LogSeverity.Verbose:
                logger.LogTrace($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
                break;

            case LogSeverity.Debug:
                logger.LogDebug($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
                break;
        }
        return Task.CompletedTask;
    }
}