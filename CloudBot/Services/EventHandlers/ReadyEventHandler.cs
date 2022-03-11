using CloudBot.Services.CommandModules;
using CloudBot.Settings;

using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace CloudBot.Services.EventHandlers;

public class ReadyEventHandler : IDiscordClientEventHandler
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;
    private readonly IOptionsMonitor<DebugSettings> debug;
    private readonly IOptionsMonitor<ConnectionSettings> connection;

    public ReadyEventHandler(IServiceProvider services, ILoggerFactory loggerFactory, IOptionsMonitor<DebugSettings> debug, IOptionsMonitor<ConnectionSettings> connection)
    {
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
        this.services = services;
        this.debug = debug;
        this.connection = connection;
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.Ready += async () => await OnReady(client);
    }

    public async Task OnReady(DiscordSocketClient client)
    {
        var guild = client.GetGuild(connection.CurrentValue.GuildId);
        logger.LogInformation("Connected to Guild {name} [{id}]", guild.Name, guild.Id);

        if (debug.CurrentValue.RefreshCommands)
        {
            await RefreshCommandsRegistration(client, guild);
        }
    }

    private async Task RefreshCommandsRegistration(DiscordSocketClient client, SocketGuild guild)
    {
        var commands = await client.GetGlobalApplicationCommandsAsync();
        List<Task> tasks = new List<Task>();
        foreach (var c in commands)
        {
            tasks.Add(c.DeleteAsync());
        }
        await Task.WhenAll(tasks);

        tasks.Clear();
        commands = await guild.GetApplicationCommandsAsync();
        foreach (var c in commands)
        {
            tasks.Add(c.DeleteAsync());
        }
        await Task.WhenAll(tasks);

        tasks.Clear();
        var slashCommandModules = services.GetServices<ISlashCommandModule>();
        foreach (var module in slashCommandModules)
        {
            tasks.Add(module.Register(client, guild, false));
        }
        await Task.WhenAll(tasks);
    }
}