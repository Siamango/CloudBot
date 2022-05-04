using BetterHaveIt.Repositories;
using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;

namespace CloudBot.Services.CommandModules;

public class AdminCommandModule : AbstractCommandModule
{
    private readonly IRepository<PreferencesModel> preferencesRepo;

    public AdminCommandModule(IRepository<PreferencesModel> prefrencesRepo, ILogger<AdminCommandModule> logger) : base(logger)
    {
        preferencesRepo = prefrencesRepo;
    }

    protected override void BuildCommands(List<SlashCommandDefinition> commands)
    {
        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("mintinfo")
            .WithDescription("Get the current mint info")
            .Build(),
            MintInfo, true));

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
        var embedBuilder = new EmbedBuilder();
        var channel = command.Data.Options.FirstOrDefault(o => o.Name.Equals("channel"));
        var add = command.Data.Options.FirstOrDefault(o => o.Name.Equals("add"));
        if (channel is null || add is null || preferencesRepo is null)
        {
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Failure", "Unable to fetch services");
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

    private async Task MintInfo(SocketSlashCommand command)
    {
        var embedBuilder = new EmbedBuilder();
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
}