using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using SolmangoNET.Models;
using System.Text.Json;

namespace CloudBot.CommandModules;

public class CommandModule : AbstractCommandModule
{
    private readonly HttpClient httpClient;

    public CommandModule(ILogger<CommandModule> logger) : base(logger)
    {
        httpClient = new HttpClient();
    }

    protected override void BuildCommands(List<SlashCommandDefinition> commands)
    {
        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("status")
            .WithDescription("Get your status")
            .AddOption("address", ApplicationCommandOptionType.String, "The member address", true)
            .Build(),
            GetMember));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("invites")
            .WithDescription("Get the user invites amount")
            .Build(),
            GetInvites));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("rarity-score")
            .WithDescription("Get the rarity score of a Cloudy")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The # number of the Cloudy", true)
            .Build(),
            GetRarityScore));
    }

    private async Task GetInvites(SocketSlashCommand command)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            var invites = await guildUser.Guild.GetInvitesAsync();
            int uses = invites.Where(i => i.Inviter.Id.Equals(guildUser.Id)).Sum(i => i.Uses) ?? 0;
            await command.RespondAsync($"{guildUser.Mention} You invited {uses} Citizens 😎");
        }
    }

    private async Task GetRarityScore(SocketSlashCommand command)
    {
        var id = command.Data.Options.FirstOrDefault(o => o.Name.Equals("id"));

        var metaTask = httpClient.GetAsync($"{Endpoints.METADATA}{$"?id={id!.Value}"}");
        var statusTask = httpClient.GetAsync($"{Endpoints.STATUS}");
        await Task.WhenAll(metaTask, statusTask);
        var statusResponse = statusTask.Result;
        var metaResponse = metaTask.Result;
        if (!statusResponse.IsSuccessStatusCode || !metaResponse.IsSuccessStatusCode)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"```API endpoint error```");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return;
        }
        var orgStatus = JsonConvert.DeserializeObject<OrganizationStatusModel>(await statusResponse.Content.ReadAsStringAsync());
        string res = await metaResponse.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<TokenMetadataModel>(res);
        if (orgStatus == null || model == null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"Wrong backend data");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
        else
        {
            int score = model.RarityScore;
            float percentage = (100F * score) / orgStatus.Collection.Supply;
            string emoji = string.Empty;
            if (percentage <= 75)
            {
                emoji = "\n🥉 ";
            }
            if (percentage <= 50)
            {
                emoji = "\n🥈 ";
            }
            if (percentage <= 10)
            {
                emoji = "\n🥇 ";
            }
            if (percentage <= 2)
            {
                emoji = "\n🏆 Congratulations! 🏆 ";
            }
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Constants.AccentColorFirst);
            embedBuilder.WithImageUrl(model.Image);
            embedBuilder.AddField($"🚀 Neon Cloud **#{id!.Value}** rarity score 🚀", $" {score}/{orgStatus.Collection.Supply}\n{emoji}\nTop **{percentage:0.00}%**");
            foreach (var attr in model.Attributes)
            {
                embedBuilder.AddField($"{attr.Trait}: {attr.Value}", $"{attr.Rarity}%  have this trait");
            }
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
    }

    private async Task GetMember(SocketSlashCommand command)
    {
        var address = command.Data.Options.FirstOrDefault(o => o.Name.Equals("address"));

        string endpoint = $"{Endpoints.MEMBERS}{$"?address={address!.Value}"}";
        var response = await httpClient.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            var jsonElement = JsonConvert.DeserializeObject<JsonElement>(await response.Content.ReadAsStringAsync());
            embedBuilder.AddField("Error", $"```json\n{JsonConvert.SerializeObject(jsonElement, Formatting.Indented)}```");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return;
        }

        string res = await response.Content.ReadAsStringAsync();
        var model = JsonConvert.DeserializeObject<MemberModel>(res);
        if (model == null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"Wrong backend data");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
        else
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Green);
            embedBuilder.AddField("Result", $"```json\n{JsonConvert.SerializeObject(model, Formatting.Indented)}```");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
    }
}