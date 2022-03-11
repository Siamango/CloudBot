using Discord;
using Discord.WebSocket;

namespace CloudBot.Services.CommandModules;

public class SlashCommandDefinition
{
    public SlashCommandProperties Properties { get; init; }

    public Func<SocketSlashCommand, Task> Delegate { get; init; }

    public SlashCommandDefinition(SlashCommandProperties properties, Func<SocketSlashCommand, Task> @delegate)
    {
        Properties = properties;
        Delegate = @delegate;
    }
}