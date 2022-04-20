using Discord;
using Discord.WebSocket;

namespace CloudBot.Services.CommandModules;

public abstract class AbstractCommandModule : ISlashCommandModule
{
    private readonly List<SlashCommandDefinition> commands;
    private readonly ILogger logger;

    protected AbstractCommandModule(ILogger logger)
    {
        commands = new List<SlashCommandDefinition>();
        BuildCommands(commands);
        this.logger = logger;
    }

    public SlashCommandDefinition? GetOrDefault(string name) => commands.FirstOrDefault(c => c.Properties.Name.Equals(name));

    public async Task Register(DiscordSocketClient client, SocketGuild? guild, bool global)
    {
        foreach (var command in commands)
        {
            logger.LogInformation("Registering command {c} on guild", command.Properties.Name);
            if (guild is not null)
            {
                await guild.CreateApplicationCommandAsync(command.Properties);
            }
            if (global)
            {
                await client.CreateGlobalApplicationCommandAsync(command.Properties);
            }
        }
    }

    protected abstract void BuildCommands(List<SlashCommandDefinition> commands);
}