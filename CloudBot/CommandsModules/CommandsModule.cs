using CloudBot.Statics;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace CloudBot.CommandsModules;

public class CommandsModule : ModuleBase<SocketCommandContext>
{
    private readonly ILogger logger;
    private readonly CommandService commandsService;

    public CommandsModule(CommandService commandsService, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger("EndpointModule");
        this.commandsService = commandsService;
    }

    [Summary("Display what I can do")]
    [Command("Help")]
    public async Task Help()
    {
        List<CommandInfo> commands = new List<CommandInfo>();
        List<CommandInfo> adminCommands = new List<CommandInfo>();
        EmbedBuilder commandsEmbedBuilder = new EmbedBuilder();
        EmbedBuilder adminCommandsEmbedBuilder = new EmbedBuilder();
        commandsEmbedBuilder.WithColor(Constants.AccentColorFirst);
        adminCommandsEmbedBuilder.WithColor(Constants.AccentColorSecond);

        foreach (CommandInfo command in commandsService.Commands.ToList())
        {
            bool adminOnly = command.Preconditions.FirstOrDefault(a => a is RequireUserPermissionAttribute attribute && attribute.GuildPermission == GuildPermission.Administrator) is not null;
            string tile = command.Name;
            string embedFieldText = command.Summary ?? "No description available\n";
            if (adminOnly)
            {
                adminCommandsEmbedBuilder.AddField(tile, embedFieldText);
            }
            else
            {
                commandsEmbedBuilder.AddField(tile, embedFieldText);
            }
        }
        await ReplyAsync("This is what I can do: ", false, commandsEmbedBuilder.Build());
        await ReplyAsync("This is for admins: ", false, adminCommandsEmbedBuilder.Build());
    }
}