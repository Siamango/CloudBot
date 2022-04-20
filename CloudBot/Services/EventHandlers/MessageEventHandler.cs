using BetterHaveIt.Repositories;
using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using SolmangoNET.Rpc;
using Solnet.Rpc;

namespace CloudBot.Services.EventHandlers;

public class MessageEventHandler : IDiscordClientEventHandler
{
    private static readonly string[] randomAutoReactEmoji = new string[] {
        "😎", "🧐", "🤠", "🤯", "🥳",
        "🤖", "🙊", "💯", "✌", "🤟",
        "🙌", "🦾", "👀", "🦍", "🐂",
        "🍹", "🌆", "🌇", "🌃", "🌩",
        "🏆", "🕶", "📣", "💸", "📬",
        "🗿", "☑", "🤑" };
    private static readonly Emoji successEmoji = new Emoji("☑");
    private static readonly Emoji failureEmoji = new Emoji("❌");
    private readonly ILogger logger;
    private readonly HttpClient httpClient;
    private readonly IRpcScheduler scheduler;
    private readonly IRpcClient rpcClient;
    private readonly IRepository<PreferencesModel> preferencesRepo;
    private readonly IRepository<WhitelistPreferencesModel> whitelistPrefRepo;
    private readonly IRepository<List<string>> addressesRepo;

    public MessageEventHandler(IRpcScheduler scheduler, IRpcClient rpcClient, ILoggerFactory loggerFactory, IRepository<PreferencesModel> preferencesRepo, IRepository<WhitelistPreferencesModel> whitelistPrefRepo, IRepository<List<string>> addressesRepo)
    {
        httpClient = new HttpClient();
        this.scheduler = scheduler;
        this.rpcClient = rpcClient;
        this.preferencesRepo = preferencesRepo;
        this.whitelistPrefRepo = whitelistPrefRepo;
        this.addressesRepo = addressesRepo;
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.MessageReceived += async (message) => await HandleMessageAsync(client, message);
    }

    private async Task HandleWhitelistList(SocketMessage message)
    {
        if (message.Channel.Id != whitelistPrefRepo.Data.ListenChannelId) return;
        if (addressesRepo.Data.Count >= whitelistPrefRepo.Data.MaxSize)
        {
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        var address = message.Content;
        if (!await rpcClient.IsAddressValid(address))
        {
            await message.AddReactionAsync(failureEmoji);
            return;
        }
        if (!addressesRepo.Data.Contains(message.Content))
        {
            addressesRepo.Data.Add(message.Content);
            addressesRepo.SaveAsync();
        }
        await message.AddReactionAsync(successEmoji);
    }

    private async Task HandleAutoReact(SocketMessage message)
    {
        if (preferencesRepo is null) return;
        var channelId = message.Channel.Id;
        if (!preferencesRepo.Data.AutoEmojiReactChannels.Contains(channelId)) return;
        var rand = new Random();
        var tasks = new List<Task>();
        for (int i = 0; i < randomAutoReactEmoji.Length; i++)
        {
            if (tasks.Count >= 20) break;
            if (rand.NextDouble() < 0.5F)
            {
                tasks.Add(message.AddReactionAsync(new Emoji(randomAutoReactEmoji[i])));
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task HandleMessageAsync(DiscordSocketClient client, SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage userMessage) return;
        if (userMessage.Author.Id == client.CurrentUser.Id || userMessage.Author.IsBot) return;
        await Task.WhenAll(HandleWhitelistList(socketMessage), HandleAutoReact(socketMessage));
    }
}