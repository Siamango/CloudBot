using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.Commands;

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

    [Summary("Retrieve members data. Put an address to query for a certain address")]
    [Command("members")]
    public async Task WhitelistedAsync([Remainder] string? address = null)
    {
        string endpoint = $"{Endpoints.MEMBERS}{(address == null ? string.Empty : $"?address={address}")}";
        var response = await httpClient.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            embedBuilder.AddField("Error", $"```json\n{JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions() { WriteIndented = true, PropertyNameCaseInsensitive = true })}```");
            await ReplyAsync(null, false, embedBuilder.Build());
            return;
        }

        string res = await response.Content.ReadAsStringAsync();
        var model = JsonSerializer.Deserialize(
            res, address == null ? typeof(MembersCountersModel) : typeof(MemberModel),
            new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        if (model == null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"Wrong backend data");
            await ReplyAsync(null, false, embedBuilder.Build());
        }
        else
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Green);
            embedBuilder.AddField("Result", $"```json\n{JsonSerializer.Serialize(model, new JsonSerializerOptions() { WriteIndented = true })}```");
            await ReplyAsync(null, false, embedBuilder.Build());
        }
    }
}