using Discord;
using Discord.WebSocket;

namespace CloudBot.Services.CommandModules;

public class SlashCommandDefinition
{
    public SlashCommandProperties Properties { get; init; }

    public Func<SocketSlashCommand, Task> Delegate { get; init; }

    public bool RequiresAdministrator { get; set; }

    public SlashCommandDefinition(SlashCommandProperties properties, Func<SocketSlashCommand, Task> commandDelegate, bool requiresAdministrator = false)
    {
        RequiresAdministrator = requiresAdministrator;
        Properties = properties;
        Delegate = commandDelegate;
    }
}