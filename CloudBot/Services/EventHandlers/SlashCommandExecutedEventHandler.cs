using CloudBot.Services.CommandModules;
using Discord;
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
        EmbedBuilder embedBuilder;
        if (slashCommand.User is not SocketGuildUser guildUser)
        {
            embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Internal error", $"User is not guild user");
            await slashCommand.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return;
        }
        foreach (var module in slashCommandModules)
        {
            var selected = module.GetOrDefault(slashCommand.CommandName);
            if (selected is null) continue;
            if (selected.RequiresAdministrator && !guildUser.GuildPermissions.Administrator)
            {
                embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(Color.Red);
                embedBuilder.AddField("Forbidden", $"You do not have the permission to run the command");
                await slashCommand.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
                return;
            }
            await selected.Delegate(slashCommand);
            return;
        }
        embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Red);
        embedBuilder.AddField("Error", $"Command not found");
        await slashCommand.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
    }
}