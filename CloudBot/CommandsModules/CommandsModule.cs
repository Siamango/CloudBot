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
        List<CommandInfo> commands = commandsService.Commands.ToList();
        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Constants.AccentColor);
        foreach (CommandInfo command in commands)
        {
            bool adminOnly = command.Preconditions.FirstOrDefault(a => a is RequireUserPermissionAttribute attribute && attribute.GuildPermission == GuildPermission.Administrator) is not null;
            string tile = command.Name;
            string embedFieldText = command.Summary ?? "No description available\n";
            if (adminOnly)
            {
                tile += " | `admin`";
            }
            embedBuilder.AddField(tile, embedFieldText);
        }
        await ReplyAsync("This is what I can do: ", false, embedBuilder.Build());
    }
}