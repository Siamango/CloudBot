using Discord;
using Discord.WebSocket;

namespace CloudBot.CommandModules;

public class SlashCommand
{
    public SlashCommandProperties Properties { get; init; }

    public Func<SocketSlashCommand, Task> Delegate { get; init; }

    public SlashCommand(SlashCommandProperties properties, Func<SocketSlashCommand, Task> @delegate)
    {
        Properties = properties;
        Delegate = @delegate;
    }
}