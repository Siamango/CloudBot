using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text.Json;

namespace CloudBot.CommandModules;

public class EndpointCommandModule : AbstractCommandModule
{
    private readonly ILogger logger;
    private readonly HttpClient httpClient;

    public EndpointCommandModule(ILoggerFactory loggerFactory)
    {
        httpClient = new HttpClient();
        logger = loggerFactory.CreateLogger("EndpointModule");
    }

    protected override void BuildCommands(List<SlashCommand> commands)
    {
        commands.Add(new SlashCommand(
            new SlashCommandBuilder()
            .WithName("status")
            .WithDescription("Get your status")
            .AddOption("address", ApplicationCommandOptionType.String, "The member address", true)
            .Build(),
            GetMembers));
    }

    private async Task GetMembers(SocketSlashCommand command)
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