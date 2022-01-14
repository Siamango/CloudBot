using Discord;
using Discord.WebSocket;

namespace CloudBot.CommandModules;

public abstract class AbstractCommandModule : ISlashCommandModule
{
    private readonly List<SlashCommand> commands;

    protected AbstractCommandModule()
    {
        commands = new List<SlashCommand>();
        BuildCommands(commands);
    }

    public SlashCommand? GetOrDefault(string name) => commands.FirstOrDefault(c => c.Properties.Name.Equals(name));

    public async Task Register(DiscordSocketClient client, SocketGuild? guild, bool global)
    {
        foreach (var command in commands)
        {
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

    protected async Task<bool> HasPermission(SocketSlashCommand command)
    {
        if (command.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator)
        {
            return true;
        }
        else
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Forbidden", $"You do not have the permission to run the command");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return false;
        }
    }

    protected abstract void BuildCommands(List<SlashCommand> commands);
}