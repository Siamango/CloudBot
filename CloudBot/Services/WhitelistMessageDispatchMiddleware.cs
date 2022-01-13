using BetterHaveIt.Repositories;
using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace CloudBot.Services;

public class WhitelistMessageDispatchMiddleware : IMessageDispatchMiddleware
{
    private static readonly Emoji successEmoji = new Emoji("☑");
    private static readonly Emoji failureEmoji = new Emoji("❌");
    private readonly ILogger logger;
    private readonly HttpClient httpClient;
    private readonly IRepository<WhitelistPreferencesModel> whitelistPrefRepo;

    public WhitelistMessageDispatchMiddleware(ILoggerFactory loggerFactory, IConfiguration configuration, IRepository<WhitelistPreferencesModel> whitelistPrefRepo)
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Api-Key", configuration.GetValue<string>("Connection:ApiKey"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        this.whitelistPrefRepo = whitelistPrefRepo;
        logger = loggerFactory.CreateLogger("Middleware");
    }

    public async Task Handle(SocketMessage message)
    {
        if (message.Channel.Id != whitelistPrefRepo.Data.ListenChannelId) return;
        var response = await httpClient.GetAsync($"{Endpoints.MEMBERS}?address={message.Content}");
        if (!response.IsSuccessStatusCode)
        {
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        var model = JsonConvert.DeserializeObject<MemberModel>(await response.Content.ReadAsStringAsync());
        if (model == null)
        {
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        if (model.Whitelisted == true)
        {
            await message.AddReactionAsync(successEmoji);
            return;
        }

        response = await httpClient.GetAsync($"{Endpoints.MEMBERS}");
        if (!response.IsSuccessStatusCode)
        {
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        var members = JsonConvert.DeserializeObject<MembersCountersModel>(await response.Content.ReadAsStringAsync());
        if (members is null || members.WhitelistedCount >= whitelistPrefRepo.Data.MaxSize)
        {
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        model.Whitelisted = true;
        response = await httpClient.PutAsync($"{Endpoints.MEMBERS}", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            await message.AddReactionAsync(failureEmoji);
        }
        else
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                IRole? pendingRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Name.Equals(whitelistPrefRepo.Data.PendingRoleId));
                IRole? confirmedRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Name.Equals(whitelistPrefRepo.Data.ConfirmedRoleId));
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
}