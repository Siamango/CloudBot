using BetterHaveIt.Repositories;
using CloudBot.Statics;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CloudBot.CommandsModules;

public class AdminCommandsModule : ModuleBase<SocketCommandContext>
{
    private readonly IRepository<Preferences> preferencesRepo;
    private readonly ILogger logger;
    private readonly CommandService commandsService;

    public AdminCommandsModule(CommandService commandsService, ILoggerFactory loggerFactory, IRepository<Preferences> preferencesRepo)
    {
        logger = loggerFactory.CreateLogger("EndpointModule");
        this.preferencesRepo = preferencesRepo;
        this.commandsService = commandsService;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Set the whitelist confirmation channel")]
    [Command("wlchannel")]
    public async Task WhitelistChannel([Remainder] SocketTextChannel? channel = null)
    {
        if (preferencesRepo is not null)
        {
            preferencesRepo.Data.WhitelistChannelName = channel == null ? string.Empty : channel.Name;
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Constants.AccentColorFirst);
            embedBuilder.AddField("Whitelist confirmation channel", $"<#{(channel == null ? "none" : channel.Id)}>");
            await ReplyAsync(null, false, embedBuilder.Build());
            preferencesRepo.SaveAsync();
        }
        await Task.CompletedTask;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Set the whitelist max size")]
    [Command("wlsize")]
    public async Task WhitelistSize([Remainder] string message)
    {
        if (preferencesRepo is not null)
        {
            if (!int.TryParse(message, out int size))
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(Color.Red);
                embedBuilder.AddField("Error", $"Size must be an integer");
                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                preferencesRepo.Data.WhitelistSize = size;
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(Color.Green);
                embedBuilder.AddField("Whitelist size", $"{size} spots");
                await ReplyAsync(null, false, embedBuilder.Build());
                preferencesRepo.SaveAsync();
            }
        }
        await Task.CompletedTask;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Set the whitelist pending and confirmed roles")]
    [Command("wlroles")]
    public async Task WhitelistRoles(SocketRole? pendingRole = null, SocketRole? confirmedRole = null)
    {
        if (preferencesRepo is not null)
        {
            preferencesRepo.Data.WhitelistPendingRoleName = pendingRole == null ? string.Empty : pendingRole.Name;
            preferencesRepo.Data.WhitelistConfirmedRoleName = confirmedRole == null ? string.Empty : confirmedRole.Name;
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Constants.AccentColorFirst);
            embedBuilder.AddField("Whitelisted pending role", (pendingRole != null ? pendingRole.Name : "none"), true);
            embedBuilder.AddField("Whitelisted confirmed role", (confirmedRole != null ? confirmedRole.Name : "none"), true);
            await ReplyAsync(null, false, embedBuilder.Build());
            preferencesRepo.SaveAsync();
        }
        await Task.CompletedTask;
    }
}