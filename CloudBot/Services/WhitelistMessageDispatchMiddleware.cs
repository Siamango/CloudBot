using BetterHaveIt.Repositories;
using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudBot.Services;

public class WhitelistMessageDispatchMiddleware : IMessageDispatchMiddleware
{
    private static readonly Emoji successEmoji = new Emoji("☑");
    private static readonly Emoji failureEmoji = new Emoji("❌");

    private readonly HttpClient httpClient;
    private readonly IRepository<Preferences> preferencesRepository;

    public WhitelistMessageDispatchMiddleware(IConfiguration configuration, IRepository<Preferences> preferencesRepository)
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Api-Key", configuration.GetValue<string>("Connection:ApiKey"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        this.preferencesRepository = preferencesRepository;
    }

    public async Task Handle(SocketMessage message)
    {
        Regex whitelistChannelRegex = new Regex(preferencesRepository.Data.WhitelistChannelName);
        if (preferencesRepository.Data.WhitelistChannelName == string.Empty || !whitelistChannelRegex.IsMatch(message.Channel.Name)) return;
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
        if (members is null || members.WhitelistedCount >= preferencesRepository.Data.WhitelistSize)
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
                IRole? pendingRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Name.Equals(preferencesRepository.Data.WhitelistPendingRoleName));
                IRole? confirmedRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Name.Equals(preferencesRepository.Data.WhitelistConfirmedRoleName));
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