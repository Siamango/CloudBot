﻿using BetterHaveIt.Repositories;
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
    private readonly IServiceProvider services;

    public CommandModule(ILogger<CommandModule> logger, IServiceProvider services) : base(logger)
    {
        httpClient = new HttpClient();
        this.services = services;
    }

    protected override void BuildCommands(List<SlashCommandDefinition> commands)
    {
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

        int idInt = Convert.ToInt32(id!.Value);
        IRepository<CandyMachineModel> cmRepo = services.GetRequiredService<IRepository<CandyMachineModel>>();
        EmbedBuilder embedBuilder;
        if (!cmRepo.Data.Items.TryGetValue(idInt, out var match))
        {
            embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"```ID {idInt} not found```");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return;
        }

        IRepository<List<RarityModel>> raritiesRepo = services.GetRequiredService<IRepository<List<RarityModel>>>();
        var rarity = raritiesRepo.Data.FirstOrDefault(r => r.Id == idInt);

        embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Constants.AccentColorFirst);
        var req = await httpClient.GetAsync(match.Link);

        string content = await req.Content.ReadAsStringAsync();
        var tokenMeta = JsonConvert.DeserializeObject<TokenMetadataModel>(content);
        if (rarity is not null)
        {
            double percentage = rarity.Percentage;
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
            embedBuilder.AddField($"🚀 Neon Cloud **#{idInt}** rarity score 🚀", $" {rarity.RarityOrder}/1250\n{emoji}\nTop **{percentage:0.00}%**");
        }
        if (tokenMeta is not null)
        {
            embedBuilder.WithImageUrl(tokenMeta.Image);

            foreach (var attr in tokenMeta.Attributes)
            {
                embedBuilder.AddField($"{attr.Trait}: {attr.Value}", $"{attr.Rarity}%  have this trait");
            }
        }

        await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
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