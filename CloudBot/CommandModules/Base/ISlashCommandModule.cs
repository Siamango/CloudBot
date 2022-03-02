using Discord.WebSocket;

namespace CloudBot.CommandModules;

public interface ISlashCommandModule
{
    public SlashCommandDefinition? GetOrDefault(string name);

    public Task Register(DiscordSocketClient client, SocketGuild? guild, bool global = false);
}