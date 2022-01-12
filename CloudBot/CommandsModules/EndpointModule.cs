using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace CloudBot;

public class EndpointModule : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public EndpointModule(ILoggerFactory loggerFactory)
    {
        httpClient = new HttpClient();
        logger = loggerFactory.CreateLogger("EndpointModule");
    }

    [Summary("Retrieve data for a certain address")]
    [Command("member")]
    public async Task WhitelistedAsync([Remainder] string address)
    {
        var response = await httpClient.GetAsync($"{Endpoints.MEMBERS}?address={address}");
        if (!response.IsSuccessStatusCode)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            embedBuilder.AddField("Error", $"```json\n{JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions() { WriteIndented = true, PropertyNameCaseInsensitive = true })}```");
            await ReplyAsync(null, false, embedBuilder.Build());
        }
        else
        {
            string res = await response.Content.ReadAsStringAsync();
            var model = JsonSerializer.Deserialize<MemberModel>(res, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            if (model == null)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(Color.Red);
                embedBuilder.AddField("Error", $"No user found, well this is an exception");
                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(Color.Green);
                embedBuilder.AddField("Member", $"```json\n{JsonSerializer.Serialize(model, new JsonSerializerOptions() { WriteIndented = true })}```");
                await ReplyAsync(null, false, embedBuilder.Build());
            }
        }
    }
}