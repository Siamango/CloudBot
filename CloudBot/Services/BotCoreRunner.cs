using BetterHaveIt.Repositories;
using CloudBot.CommandModules;
using CloudBot.Statics;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CloudBot.Services;

public class BotCoreRunner : ICoreRunner
{
    private readonly IServiceProvider services;
    private readonly IEnumerable<IMessageDispatchMiddleware> dispatchMiddlewares;
    private readonly IConfiguration configuration;
    private readonly ILogger logger;
    private readonly CommandService commandService;
    private readonly DiscordSocketClient client;
    private readonly IRepository<WhitelistPreferencesModel>? whitelistPrefRepo;

    public BotCoreRunner(IServiceProvider services, CommandService commandService, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger("Core");
        whitelistPrefRepo = services.GetService<IRepository<WhitelistPreferencesModel>>();
        dispatchMiddlewares = services.GetServices<IMessageDispatchMiddleware>();
        logger.LogInformation("Injecting {n} middlewares", dispatchMiddlewares.Count());
        this.configuration = configuration;

        logger.LogInformation("Token: {token}", configuration.GetValue<string>("Connection:DiscordToken"));
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogGatewayIntentWarnings = false,
            MessageCacheSize = 128,
            ConnectionTimeout = 5000,
            HandlerTimeout = null,
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info,
        });
        this.commandService = commandService;
        this.commandService.Log += Log;
        client.Log += Log;

        client.Ready += OnReady;
        client.MessageReceived += HandleMessageAsync;
        client.UserJoined += OnUserJoined;
        client.SlashCommandExecuted += SlashCommandExecuted;
        client.SetGameAsync("⚡ Zapping Citizens ⚡");
        this.services = services;
    }

    public async Task RunAsync()
    {
        logger.LogInformation("Environment {environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        await client.LoginAsync(TokenType.Bot, configuration.GetValue<string>("Connection:DiscordToken"));
        await client.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }

    public async Task OnReady()
    {
        var guild = client.GetGuild(configuration.GetValue<ulong>("Connection:GuildId"));
        logger.LogInformation("Connected to Guild {name}[{id}]", guild.Name, guild.Id);
        logger.LogInformation("Caching endpoints");
        await new HttpClient().GetAsync($"{Endpoints.STATUS}");
        //var commands = await client.GetGlobalApplicationCommandsAsync();
        //foreach (var c in commands)
        //{
        //    await c.DeleteAsync();
        //}

        //commands = await guild.GetApplicationCommandsAsync();
        //foreach (var c in commands)
        //{
        //    await c.DeleteAsync();
        //}
        //var slashCommandModules = services.GetServices<ISlashCommandModule>();
        //Task[] tasks = new Task[slashCommandModules.Count()];
        //for (int i = 0; i < tasks.Length; i++)
        //{
        //    tasks[i] = slashCommandModules.ElementAt(i).Register(client, guild, false);
        //}
        //await Task.WhenAll(tasks);
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        if (whitelistPrefRepo is null) return;
        if (!whitelistPrefRepo.Data.InvitesRewardEnabled) return;
        var invites = await user.Guild.GetInvitesAsync();
        IRole? pendingRole = user.Guild.Roles.FirstOrDefault(r => r.Id.Equals(whitelistPrefRepo.Data.PendingRoleId));
        List<SocketGuildUser> guildUsers = new List<SocketGuildUser>();
        foreach (var i in invites)
        {
            if (i.Uses >= whitelistPrefRepo.Data.InvitesRequired)
            {
                var guildUser = user.Guild.GetUser(i.Inviter.Id);
                if (guildUser.Roles.FirstOrDefault(r => r.Id == whitelistPrefRepo.Data.ConfirmedRoleId || r.Id == whitelistPrefRepo.Data.PendingRoleId) != default)
                {
                    continue;
                }
                if (pendingRole != null)
                {
                    await guildUser.AddRoleAsync(pendingRole);
                }
                var channel = guildUser.Guild.GetTextChannel(whitelistPrefRepo.Data.AnnouncementsChannelId);
                if (channel != null)
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.WithColor(Color.Green);
                    embedBuilder.AddField($"🏆 Congratulations 🏆", $"{guildUser.Mention}, you earned yourself a **whitelist** spot!");
                    await channel.SendMessageAsync(null, false, embedBuilder.Build());
                }
            }
        }
    }

    private async Task SlashCommandExecuted(SocketSlashCommand slashCommand)
    {
        var slashCommandModules = services.GetServices<ISlashCommandModule>();
        foreach (var module in slashCommandModules)
        {
            var selected = module.GetOrDefault(slashCommand.CommandName);
            if (selected is not null)
            {
                await selected.Delegate(slashCommand);
                break;
            }
        }
    }

    private async Task HandleMessageAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage userMessage) return;

        if (userMessage.Author.Id == client.CurrentUser.Id || userMessage.Author.IsBot) return;

        foreach (var middleware in dispatchMiddlewares)
        {
            _ = middleware.Handle(socketMessage);
        }
        await Task.CompletedTask;
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