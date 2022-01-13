using Discord.WebSocket;

namespace CloudBot.CommandModules;

public interface ISlashCommandModule
{
    public SlashCommand? GetOrDefault(string name);

    public Task Register(DiscordSocketClient client, SocketGuild? guild, bool global = false);
}