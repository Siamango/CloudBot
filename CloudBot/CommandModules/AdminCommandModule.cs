using BetterHaveIt.Repositories;
using Discord;
using Discord.WebSocket;

namespace CloudBot.CommandModules;

public class AdminCommandModule : AbstractCommandModule
{
    private readonly IServiceProvider services;
    private readonly IRepository<WhitelistPreferencesModel> preferencesRepo;
    private readonly ILogger logger;

    public AdminCommandModule(IServiceProvider services, IRepository<WhitelistPreferencesModel> preferencesRepo, ILogger<AdminCommandModule> logger) : base()
    {
        this.services = services;
        this.preferencesRepo = preferencesRepo;
        this.logger = logger;
    }

    protected override void BuildCommands(List<SlashCommand> commands)
    {
        commands.Add(new SlashCommand(
            new SlashCommandBuilder()
            .WithName("wlsetup")
            .WithDescription("Set the whitelist framework")
            .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to listen for whitelist addresses", false)
            .AddOption("size", ApplicationCommandOptionType.Integer, "Set the maximum size for the whitelist", false)
            .AddOption("pending_role", ApplicationCommandOptionType.Role, "The whitelist pending role", false)
            .AddOption("invites", ApplicationCommandOptionType.Integer, "The number of invites to obtain the pending whitelist role", false)
            .AddOption("confirmed_role", ApplicationCommandOptionType.Role, "The whitelist confirmed role", false)
            .AddOption("announcements", ApplicationCommandOptionType.Channel, "The channel in which announcements are made", false)
            .Build(),
            SetupWhitelistFramework));

        commands.Add(new SlashCommand(
            new SlashCommandBuilder()
            .WithName("wlget")
            .WithDescription("Get the current whitelist setup")
            .Build(),
            GetWhitelistFramework));
    }

    private async Task GetWhitelistFramework(SocketSlashCommand command)
    {
        if (!await HasPermission(command)) return;
        if (preferencesRepo is null) return;

        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Green);
        embedBuilder.AddField("Whitelist confirmation channel", $"<#{preferencesRepo.Data.ListenChannelId}>");
        embedBuilder.AddField("Whitelist size", $"{preferencesRepo.Data.MaxSize}");
        embedBuilder.AddField("Whitelist enable invites rewards", $"{preferencesRepo.Data.InvitesRewardEnabled}");
        embedBuilder.AddField("Whitelist invites required", $"{preferencesRepo.Data.InvitesRequired}");
        embedBuilder.AddField("Whitelist pending role", $"<@&{preferencesRepo.Data.PendingRoleId}>");
        embedBuilder.AddField("Whitelist confirmed role", $"<@&{preferencesRepo.Data.ConfirmedRoleId}>");
        embedBuilder.AddField("Whitelist announcements channel", $"<#{preferencesRepo.Data.AnnouncementsChannelId}>");
        await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        await Task.CompletedTask;
    }

    private async Task SetupWhitelistFramework(SocketSlashCommand command)
    {
        if (!await HasPermission(command)) return;
        if (preferencesRepo is null) return;

        var channel = command.Data.Options.FirstOrDefault(o => o.Name.Equals("channel"));
        var size = command.Data.Options.FirstOrDefault(o => o.Name.Equals("size"));
        var invites = command.Data.Options.FirstOrDefault(o => o.Name.Equals("invites"));
        var pendingRole = command.Data.Options.FirstOrDefault(o => o.Name.Equals("pending_role"));
        var confirmedRole = command.Data.Options.FirstOrDefault(o => o.Name.Equals("confirmed_role"));
        var announcements = command.Data.Options.FirstOrDefault(o => o.Name.Equals("announcements"));

        preferencesRepo.Data.ListenChannelId = channel == null ? 0 : ((SocketChannel)channel.Value).Id;
        preferencesRepo.Data.MaxSize = size == null ? 0 : Convert.ToInt32(size.Value);
        preferencesRepo.Data.InvitesRequired = invites == null ? -1 : Convert.ToInt32(invites.Value);
        preferencesRepo.Data.InvitesRewardEnabled = preferencesRepo.Data.InvitesRequired > 0;
        preferencesRepo.Data.PendingRoleId = pendingRole == null ? 0 : ((SocketRole)pendingRole.Value).Id;
        preferencesRepo.Data.ConfirmedRoleId = confirmedRole == null ? 0 : ((SocketRole)confirmedRole.Value).Id;
        preferencesRepo.Data.AnnouncementsChannelId = announcements == null ? 0 : ((SocketChannel)announcements.Value).Id;

        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Green);
        embedBuilder.AddField("Whitelist confirmation channel", $"<#{preferencesRepo.Data.ListenChannelId}>");
        embedBuilder.AddField("Whitelist size", $"{preferencesRepo.Data.MaxSize}");
        embedBuilder.AddField("Whitelist enable invites rewards", $"{preferencesRepo.Data.InvitesRewardEnabled}");
        embedBuilder.AddField("Whitelist invites required", $"{preferencesRepo.Data.InvitesRequired}");
        embedBuilder.AddField("Whitelist pending role", $"<@&{preferencesRepo.Data.PendingRoleId}>");
        embedBuilder.AddField("Whitelist confirmed role", $"<@&{preferencesRepo.Data.ConfirmedRoleId}>");
        embedBuilder.AddField("Whitelist announcements channel", $"<#{preferencesRepo.Data.AnnouncementsChannelId}>");
        await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });

        preferencesRepo.SaveAsync();
        await Task.CompletedTask;
    }
}