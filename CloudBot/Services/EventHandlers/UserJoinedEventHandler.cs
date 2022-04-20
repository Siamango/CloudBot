using BetterHaveIt.Repositories;
using CloudBot.Models;
using Discord;
using Discord.WebSocket;

namespace CloudBot.Services.EventHandlers;

public class UserJoinedEventHandler : IDiscordClientEventHandler
{
    private readonly ILogger logger;

    private readonly IRepository<WhitelistPreferencesModel> whitelistPrefRepo;

    public UserJoinedEventHandler(ILoggerFactory loggerFactory, IRepository<WhitelistPreferencesModel> whitelistPrefRepo)
    {
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
        this.whitelistPrefRepo = whitelistPrefRepo;
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.UserJoined += async (guildUser) => await OnJoined(client, guildUser);
    }

    public async Task OnJoined(DiscordSocketClient client, SocketGuildUser user)
    {
        if (whitelistPrefRepo is null) return;
        if (!whitelistPrefRepo.Data.InvitesRewardEnabled) return;
        var invites = await user.Guild.GetInvitesAsync();
        IRole? pendingRole = user.Guild.Roles.FirstOrDefault(r => r.Id.Equals(whitelistPrefRepo.Data.PendingRoleId));
        List<SocketGuildUser> guildUsers = new List<SocketGuildUser>();
        Dictionary<ulong, int> invitesMap = new Dictionary<ulong, int>();

        foreach (var i in invites)
        {
            if (i.Uses is null) continue;
            if (invitesMap.ContainsKey(i.Inviter.Id))
            {
                invitesMap[i.Inviter.Id] += i.Uses ?? default;
            }
            else
            {
                invitesMap.Add(i.Inviter.Id, i.Uses ?? default);
            }
        }
        foreach (var pair in invitesMap)
        {
            if (pair.Value >= whitelistPrefRepo.Data.InvitesRequired)
            {
                var guildUser = user.Guild.GetUser(pair.Key);
                if (guildUser.Roles.FirstOrDefault(r => r.Id == whitelistPrefRepo.Data.ConfirmedRoleId || r.Id == whitelistPrefRepo.Data.PendingRoleId) != default)
                {
                    continue;
                }
                if (pendingRole != null)
                {
                    await guildUser.AddRoleAsync(pendingRole);
                }
                var channel = guildUser.Guild.GetTextChannel(whitelistPrefRepo.Data.AnnouncementsChannelId);
                if (channel != null)
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.WithColor(Color.Green);
                    embedBuilder.AddField($"🏆 Congratulations 🏆", $"{guildUser.Mention}, you earned yourself a **whitelist** spot!");
                    await channel.SendMessageAsync(null, false, embedBuilder.Build());
                }
            }
        }
    }
}