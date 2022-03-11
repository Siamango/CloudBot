using CloudBot.Services.CommandModules;
using Discord.WebSocket;

namespace CloudBot.Services.EventHandlers;

public class SlashCommandExecutedEventHandler : IDiscordClientEventHandler
{
    private readonly IEnumerable<ISlashCommandModule> slashCommandModules;
    private readonly ILogger logger;
    private readonly IConfiguration configuration;

    public SlashCommandExecutedEventHandler(ILoggerFactory loggerFactory, IEnumerable<ISlashCommandModule> slashCommandModules, IConfiguration configuration)
    {
        this.slashCommandModules = slashCommandModules;
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
        this.configuration = configuration;
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += async (command) => await SlashCommandExecuted(client, command);
    }

    private async Task SlashCommandExecuted(DiscordSocketClient client, SocketSlashCommand slashCommand)
    {
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
}