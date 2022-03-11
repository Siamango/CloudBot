using CloudBot.CommandModules;
using CloudBot.Statics;

using Discord.WebSocket;

namespace CloudBot.EventHandlers;

public class ReadyEventHandler : IDiscordClientEventHandler
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;
    private readonly IConfiguration configuration;

    public ReadyEventHandler(IServiceProvider services, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
        this.services = services;
        this.configuration = configuration;
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.Ready += async () => await OnReady(client);
    }

    public async Task OnReady(DiscordSocketClient client)
    {
        var guild = client.GetGuild(configuration.GetValue<ulong>("Connection:GuildId"));
        logger.LogInformation("Connected to Guild {name} [{id}]", guild.Name, guild.Id);

        await RefreshCommandsRegistration(client, guild);
    }

    private async Task RefreshCommandsRegistration(DiscordSocketClient client, SocketGuild guild)
    {
        logger.LogInformation("Caching endpoints");
        await new HttpClient().GetAsync($"{Endpoints.STATUS}");
        var commands = await client.GetGlobalApplicationCommandsAsync();
        foreach (var c in commands)
        {
            await c.DeleteAsync();
        }

        commands = await guild.GetApplicationCommandsAsync();
        foreach (var c in commands)
        {
            await c.DeleteAsync();
        }
        var slashCommandModules = services.GetServices<ISlashCommandModule>();
        Task[] tasks = new Task[slashCommandModules.Count()];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = slashCommandModules.ElementAt(i).Register(client, guild, false);
        }
        await Task.WhenAll(tasks);
    }
}