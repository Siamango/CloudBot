using BetterHaveIt.Repositories;
using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Solnet.Rpc;

namespace CloudBot.Services.CommandModules;

public class AdminCommandModule : AbstractCommandModule
{
    private readonly IServiceProvider services;
    private readonly IRpcClient rpcClient;
    private readonly IRepository<List<string>> addressesRepo;
    private readonly IRepository<PreferencesModel> preferencesRepo;
    private readonly IRepository<WhitelistPreferencesModel> wlPreferencesRepo;
    private readonly ILogger logger;
    private readonly HttpClient httpClient;

    public AdminCommandModule(IServiceProvider services, IRpcClient rpcClient, IRepository<List<string>> addressesRepo, IRepository<PreferencesModel> prefrencesRepo, IRepository<WhitelistPreferencesModel> wlPreferencesRepo, ILogger<AdminCommandModule> logger) : base(logger)
    {
        this.services = services;
        this.rpcClient = rpcClient;
        this.addressesRepo = addressesRepo;
        this.preferencesRepo = prefrencesRepo;
        this.wlPreferencesRepo = wlPreferencesRepo;
        this.logger = logger;
        httpClient = new HttpClient();
    }

    protected override void BuildCommands(List<SlashCommandDefinition> commands)
    {
        commands.Add(new SlashCommandDefinition(
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
            SetupWhitelistFramework, true));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("wlget")
            .WithDescription("Get the current whitelist setup")
            .Build(),
            GetWhitelistFramework, true));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("mintinfo")
            .WithDescription("Get the current mint info")
            .Build(),
            MintInfo, true));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("wlset")
            .WithDescription("Add a current address to the whitelist")
            .AddOption("address", ApplicationCommandOptionType.String, "The address to add", true)
            .AddOption("whitelisted", ApplicationCommandOptionType.Boolean, "The whitelist status", true)
            .Build(),
            AddToWhitelist, true));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("autoreact")
            .WithDescription("Add/remove a channel to autoreact module")
            .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel", true)
            .AddOption("add", ApplicationCommandOptionType.Boolean, "Whether to add or to remove", true)
            .Build(),
            ModifyAutoReactChannels, true));
    }

    private async Task ModifyAutoReactChannels(SocketSlashCommand command)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        var channel = command.Data.Options.FirstOrDefault(o => o.Name.Equals("channel"));
        var add = command.Data.Options.FirstOrDefault(o => o.Name.Equals("add"));
        if (channel is null || add is null || preferencesRepo is null)
        {
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Failure", "Autoreact channels: \n" + string.Join("\n", preferencesRepo.Data.AutoEmojiReactChannels));
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return;
        }
        var channelId = ((SocketChannel)channel!.Value).Id;
        var addBool = (bool)add!.Value;
        if (!addBool)
        {
            preferencesRepo.Data.AutoEmojiReactChannels.RemoveAll(c => c.Equals(channelId));
        }
        else
        {
            if (!preferencesRepo.Data.AutoEmojiReactChannels.Contains(channelId))
            {
                preferencesRepo.Data.AutoEmojiReactChannels.Add(channelId);
            }
        }

        embedBuilder.WithColor(Color.Green);
        embedBuilder.AddField("Success", "Autoreact channels: \n" + string.Join("\n", preferencesRepo.Data.AutoEmojiReactChannels));
        await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        preferencesRepo.SaveAsync();
        return;
    }

    private async Task AddToWhitelist(SocketSlashCommand command)
    {
        var address = (string)command.Data.Options.FirstOrDefault(o => o.Name.Equals("address"))!.Value;
        var whitelisted = (bool)command.Data.Options.FirstOrDefault(o => o.Name.Equals("whitelisted"))!.Value;

        if (!await rpcClient.IsAddressValid(address))
        {
            await command.RespondAsync(null, new Embed[] {
                new EmbedBuilder().WithColor(Color.Red).AddField("Failure", $"```json\nInvalid address```").Build()
            }, false, true);
            return;
        }
        if (whitelisted)
        {
            if (!addressesRepo.Data.Contains(address))
            {
                addressesRepo.Data.Add(address);
            }
        }
        else
        {
            addressesRepo.Data.Remove(address);
        }
        addressesRepo.SaveAsync();
        await command.RespondAsync(null, new Embed[] {
            new EmbedBuilder().WithColor(Color.Green).AddField("Success", $"```\n{address} new state: {whitelisted}```").Build()
        }, false, true);
    }

    private async Task MintInfo(SocketSlashCommand command)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Constants.AccentColorFirst);
        embedBuilder.WithTitle("📋 MINT DETAILS 📋");
        embedBuilder.AddField("Public sale start", "🔷 `TUE 9:00 PM UTC February 1, 2022`\n" +
            "🔷 **0.42 SOL**\n" +
            "🔷 Only at: https://neonclouds.net/ \n" +
            "🔷 1269/1569 NFTs are available to mint\n" +
            "🔷 You can mint as many as you can until sold out");
        embedBuilder.AddField("To mint", "Go to https://neonclouds.net/mint and follow the directions:");
        embedBuilder.AddField("1️⃣ ", "No need to refresh the page upon contdown end, the mint button will apper automatically");
        embedBuilder.AddField("2️⃣ ", "Connect your wallet that you would like to mint with");
        embedBuilder.AddField("3️⃣ ", "Your wallet will ask you to confirm connection. This is to verify that you own the wallet you entered. This request will not trigger a blockchain transaction or cost any gas fees, similar to common Web3 websites or Grape");
        embedBuilder.AddField("\n⚠ Important", "Care for phishing **always** double check the domain!\n" +
            "Remember to keep at least **0.05 + 0.42 SOL (single mint price)** in the wallet in order to make up for the GAS FEES");

        await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        await Task.CompletedTask;
    }

    private async Task GetWhitelistFramework(SocketSlashCommand command)
    {
        if (wlPreferencesRepo is null) return;

        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Green);
        embedBuilder.AddField("Whitelist confirmation channel", $"<#{wlPreferencesRepo.Data.ListenChannelId}>");
        embedBuilder.AddField("Whitelist size", $"{wlPreferencesRepo.Data.MaxSize}");
        embedBuilder.AddField("Whitelist enable invites rewards", $"{wlPreferencesRepo.Data.InvitesRewardEnabled}");
        embedBuilder.AddField("Whitelist invites required", $"{wlPreferencesRepo.Data.InvitesRequired}");
        embedBuilder.AddField("Whitelist pending role", $"<@&{wlPreferencesRepo.Data.PendingRoleId}>");
        embedBuilder.AddField("Whitelist confirmed role", $"<@&{wlPreferencesRepo.Data.ConfirmedRoleId}>");
        embedBuilder.AddField("Whitelist announcements channel", $"<#{wlPreferencesRepo.Data.AnnouncementsChannelId}>");
        await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        await Task.CompletedTask;
    }

    private async Task SetupWhitelistFramework(SocketSlashCommand command)
    {
        if (wlPreferencesRepo is null) return;

        var channel = command.Data.Options.FirstOrDefault(o => o.Name.Equals("channel"));
        var size = command.Data.Options.FirstOrDefault(o => o.Name.Equals("size"));
        var invites = command.Data.Options.FirstOrDefault(o => o.Name.Equals("invites"));
        var pendingRole = command.Data.Options.FirstOrDefault(o => o.Name.Equals("pending_role"));
        var confirmedRole = command.Data.Options.FirstOrDefault(o => o.Name.Equals("confirmed_role"));
        var announcements = command.Data.Options.FirstOrDefault(o => o.Name.Equals("announcements"));

        wlPreferencesRepo.Data.ListenChannelId = channel == null ? 0 : ((SocketChannel)channel.Value).Id;
        wlPreferencesRepo.Data.MaxSize = size == null ? 0 : Convert.ToInt32(size.Value);
        wlPreferencesRepo.Data.InvitesRequired = invites == null ? -1 : Convert.ToInt32(invites.Value);
        wlPreferencesRepo.Data.InvitesRewardEnabled = wlPreferencesRepo.Data.InvitesRequired > 0;
        wlPreferencesRepo.Data.PendingRoleId = pendingRole == null ? 0 : ((SocketRole)pendingRole.Value).Id;
        wlPreferencesRepo.Data.ConfirmedRoleId = confirmedRole == null ? 0 : ((SocketRole)confirmedRole.Value).Id;
        wlPreferencesRepo.Data.AnnouncementsChannelId = announcements == null ? 0 : ((SocketChannel)announcements.Value).Id;

        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Green);
        embedBuilder.AddField("Whitelist confirmation channel", $"<#{wlPreferencesRepo.Data.ListenChannelId}>");
        embedBuilder.AddField("Whitelist size", $"{wlPreferencesRepo.Data.MaxSize}");
        embedBuilder.AddField("Whitelist enable invites rewards", $"{wlPreferencesRepo.Data.InvitesRewardEnabled}");
        embedBuilder.AddField("Whitelist invites required", $"{wlPreferencesRepo.Data.InvitesRequired}");
        embedBuilder.AddField("Whitelist pending role", $"<@&{wlPreferencesRepo.Data.PendingRoleId}>");
        embedBuilder.AddField("Whitelist confirmed role", $"<@&{wlPreferencesRepo.Data.ConfirmedRoleId}>");
        embedBuilder.AddField("Whitelist announcements channel", $"<#{wlPreferencesRepo.Data.AnnouncementsChannelId}>");
        await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });

        wlPreferencesRepo.SaveAsync();
        await Task.CompletedTask;
    }
}