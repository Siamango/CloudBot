using BetterHaveIt.Repositories;
using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text;

namespace CloudBot.EventHandlers;

public class MessageEventHandler : IDiscordClientEventHandler
{
    private static readonly Emoji successEmoji = new Emoji("☑");
    private static readonly Emoji failureEmoji = new Emoji("❌");
    private readonly ILogger logger;
    private readonly HttpClient httpClient;
    private readonly IRepository<WhitelistPreferencesModel> whitelistPrefRepo;

    public MessageEventHandler(ILoggerFactory loggerFactory, IConfiguration configuration, IRepository<WhitelistPreferencesModel> whitelistPrefRepo)
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Api-Key", configuration.GetValue<string>("Connection:ApiKey"));
        this.whitelistPrefRepo = whitelistPrefRepo;
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.MessageReceived += async (message) => await HandleMessageAsync(client, message);
    }

    private async Task Handle(SocketMessage message)
    {
        logger.LogWarning("Received {message}", message.Content);
        if (message.Channel.Id != whitelistPrefRepo.Data.ListenChannelId) return;
        var response = await httpClient.GetAsync($"{Endpoints.MEMBERS}?address={message.Content}");
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Error GET [{}]: {}", response.StatusCode, await response.Content.ReadAsStringAsync());
            await message.AddReactionAsync(failureEmoji);
            return;
        }

        var model = JsonConvert.DeserializeObject<MemberModel>(await response.Content.ReadAsStringAsync());
        if (model == null)
        {
            logger.LogWarning("Error model null");
            await message.AddReactionAsync(failureEmoji);
            return;
        }

        if (model.Whitelisted == true)
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                IRole? pendingRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Id.Equals(whitelistPrefRepo.Data.PendingRoleId));
                IRole? confirmedRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Id.Equals(whitelistPrefRepo.Data.ConfirmedRoleId));
                if (pendingRole is not null)
                {
                    await guildUser.RemoveRoleAsync(pendingRole);
                }
                if (confirmedRole is not null)
                {
                    await guildUser.AddRoleAsync(confirmedRole);
                }
            }
            await message.AddReactionAsync(successEmoji);
            return;
        }

        response = await httpClient.GetAsync($"{Endpoints.MEMBERS}");
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Error GET [{}]: {}", response.StatusCode, await response.Content.ReadAsStringAsync());
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        var members = JsonConvert.DeserializeObject<MembersCountersModel>(await response.Content.ReadAsStringAsync());
        if (members is null || members.WhitelistedCount >= whitelistPrefRepo.Data.MaxSize)
        {
            logger.LogWarning("Whitelist saturated");
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        model.Whitelisted = true;
        response = await httpClient.PutAsync($"{Endpoints.MEMBERS}", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Error PUT [{}]: {}", response.StatusCode, await response.Content.ReadAsStringAsync());
            await message.AddReactionAsync(failureEmoji);
        }
        else
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                IRole? pendingRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Id.Equals(whitelistPrefRepo.Data.PendingRoleId));
                IRole? confirmedRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Id.Equals(whitelistPrefRepo.Data.ConfirmedRoleId));
                if (pendingRole is not null)
                {
                    await guildUser.RemoveRoleAsync(pendingRole);
                }
                if (confirmedRole is not null)
                {
                    await guildUser.AddRoleAsync(confirmedRole);
                }
            }
            await message.AddReactionAsync(successEmoji);
        }
        return;
    }

    private async Task HandleMessageAsync(DiscordSocketClient client, SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage userMessage) return;

        if (userMessage.Author.Id == client.CurrentUser.Id || userMessage.Author.IsBot) return;

        await Handle(socketMessage);
        await Task.CompletedTask;
    }
}