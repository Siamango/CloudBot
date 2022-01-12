using CloudBot.CommandsModules;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CloudBot.Services;

public class BotCoreRunner : ICoreRunner
{
    private readonly IServiceProvider services;
    private readonly IEnumerable<IMessageDispatchMiddleware> dispatchMiddlewares;
    private readonly IConfiguration configuration;
    private readonly ILogger<BotCoreRunner> logger;
    private readonly CommandService commandService;
    private readonly DiscordSocketClient client;

    public BotCoreRunner(IServiceProvider services, IEnumerable<IMessageDispatchMiddleware> dispatchMiddlewares, CommandService commandService, IConfiguration configuration, ILogger<BotCoreRunner> logger)
    {
        this.dispatchMiddlewares = dispatchMiddlewares;
        this.configuration = configuration;
        this.logger = logger;

        logger.LogInformation("Token: {token}", configuration.GetValue<string>("Connection:DiscordToken"));
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogLevel = LogSeverity.Info,
        });
        this.commandService = commandService;
        this.commandService.Log += Log;
        client.Log += Log;
        client.MessageReceived += HandleMessageAsync;

        client.SetGameAsync("⚡ Zapping Citizens ⚡");
        this.services = services;
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
        await commandService.AddModuleAsync<EndpointModule>(services);
        await commandService.AddModuleAsync<AdminCommandsModule>(services);
        await commandService.AddModuleAsync<CommandsModule>(services);
    }

    private async Task HandleMessageAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage userMessage) return;

        if (userMessage.Author.Id == client.CurrentUser.Id || userMessage.Author.IsBot) return;

        foreach (var middleware in dispatchMiddlewares)
        {
            await middleware.Handle(socketMessage);
        }

        int pos = 0;
        if (userMessage.HasStringPrefix("cb.", ref pos))
        {
            var context = new SocketCommandContext(client, userMessage);
            var result = await commandService.ExecuteAsync(context, pos, services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                await userMessage.Channel.SendMessageAsync(result.ErrorReason);
        }
    }

    private Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                logger.LogCritical("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Error:
                logger.LogError("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Warning:
                logger.LogWarning("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Info:
                logger.LogInformation("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Verbose:
                logger.LogTrace("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Debug:
                logger.LogDebug("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;
        }
        return Task.CompletedTask;
    }
}