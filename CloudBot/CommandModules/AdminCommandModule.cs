using BetterHaveIt.Repositories;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using SolmangoNET.Models;
using System.Text;

namespace CloudBot.CommandModules;

public class AdminCommandModule : AbstractCommandModule
{
    private readonly IServiceProvider services;
    private readonly IRepository<WhitelistPreferencesModel> preferencesRepo;
    private readonly ILogger logger;
    private readonly HttpClient httpClient;

    public AdminCommandModule(IServiceProvider services, IConfiguration configuration, IRepository<WhitelistPreferencesModel> preferencesRepo, ILogger<AdminCommandModule> logger) : base(logger)
    {
        this.services = services;
        this.preferencesRepo = preferencesRepo;
        this.logger = logger;
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Api-Key", configuration.GetValue<string>("Connection:ApiKey"));
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
            SetupWhitelistFramework));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("wlget")
            .WithDescription("Get the current whitelist setup")
            .Build(),
            GetWhitelistFramework));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("mintinfo")
            .WithDescription("Get the current mint info")
            .Build(),
            MintInfo));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("wlset")
            .WithDescription("Add a current address to the whitelist")
            .AddOption("address", ApplicationCommandOptionType.String, "The address to add", true)
            .AddOption("whitelisted", ApplicationCommandOptionType.Boolean, "The whitelist status", true)
            .Build(),
            AddToWhitelist));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("promised")
            .WithDescription("Add promised mints to an address")
            .AddOption("address", ApplicationCommandOptionType.String, "The address to add the mints to", true)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "Amount to add", true)
            .Build(),
            AddPromised));
    }

    private async Task AddPromised(SocketSlashCommand command)
    {
        if (!await HasPermission(command)) return;
        var address = command.Data.Options.FirstOrDefault(o => o.Name.Equals("address"));
        var amount = command.Data.Options.FirstOrDefault(o => o.Name.Equals("amount"));
        var response = await httpClient.GetAsync($"{Endpoints.MEMBERS}?address={address!.Value}");
        if (!response.IsSuccessStatusCode)
        {
            await command.RespondAsync(null,
                new Embed[] { new EmbedBuilder().AddField("RPC error", response.Content).Build() });
            return;
        }
        var model = JsonConvert.DeserializeObject<MemberModel>(await response.Content.ReadAsStringAsync());
        if (model == null)
        {
            await command.RespondAsync(null, new Embed[] { new EmbedBuilder().AddField("Deserialization error", response.Content).Build() });
            return;
        }

        model.Promised += Convert.ToInt32(amount!.Value);

        response = await httpClient.PutAsync($"{Endpoints.MEMBERS}",
            new StringContent(JsonConvert.SerializeObject(model, Formatting.Indented),
            Encoding.UTF8,
            "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            await command.RespondAsync(null, new Embed[] { new EmbedBuilder().AddField("RPC error", response.Content).Build() });
            return;
        }

        await command.RespondAsync(null, new Embed[] {
            new EmbedBuilder().AddField("Success", $"```json\n{JsonConvert.SerializeObject(model, Formatting.Indented)}```").Build()
        });
    }

    private async Task AddToWhitelist(SocketSlashCommand command)
    {
        if (!await HasPermission(command)) return;
        var address = command.Data.Options.FirstOrDefault(o => o.Name.Equals("address"));
        var whitelisted = command.Data.Options.FirstOrDefault(o => o.Name.Equals("whitelisted"));
        var response = await httpClient.GetAsync($"{Endpoints.MEMBERS}?address={address!.Value}");
        if (!response.IsSuccessStatusCode)
        {
            await command.RespondAsync(null, new Embed[] {
                new EmbedBuilder().WithColor(Color.Red).AddField("RPC error", await response.Content.ReadAsStringAsync()).Build()
            });
            return;
        }
        var model = JsonConvert.DeserializeObject<MemberModel>(await response.Content.ReadAsStringAsync());
        if (model == null)
        {
            await command.RespondAsync(null, new Embed[] {
                new EmbedBuilder().WithColor(Color.Red).AddField("Deserialization error", await response.Content.ReadAsStringAsync()).Build()
            });
            return;
        }

        model.Whitelisted = (bool)whitelisted!.Value;

        response = await httpClient.PutAsync($"{Endpoints.MEMBERS}",
            new StringContent(JsonConvert.SerializeObject(model),
            Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            await command.RespondAsync(null, new Embed[] {
                new EmbedBuilder().WithColor(Color.Red).AddField("RPC 2 error", await response.Content.ReadAsStringAsync()).Build()
            });
            return;
        }

        await command.RespondAsync(null, new Embed[] {
            new EmbedBuilder().WithColor(Color.Green).AddField("Success", $"```json\n{JsonConvert.SerializeObject(model, Formatting.Indented)}```").Build()
        });
    }

    private async Task MintInfo(SocketSlashCommand command)
    {
        if (!await HasPermission(command)) return;

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