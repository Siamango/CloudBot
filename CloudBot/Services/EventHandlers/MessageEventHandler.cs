using BetterHaveIt.Repositories;
using CloudBot.Models;
using Discord;
using Discord.WebSocket;

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

    private readonly ILogger logger;

    private readonly IRepository<PreferencesModel> preferencesRepo;

    public MessageEventHandler(ILoggerFactory loggerFactory, IRepository<PreferencesModel> preferencesRepo)
    {
        this.preferencesRepo = preferencesRepo;
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.MessageReceived += async (message) => await HandleMessageAsync(client, message);
    }

    private async Task HandleAutoReact(SocketMessage message)
    {
        if (preferencesRepo is null) return;
        var channelId = message.Channel.Id;
        if (!preferencesRepo.Data.AutoEmojiReactChannels.Contains(channelId)) return;
        var rand = new Random();
        var tasks = new List<Task>();
        for (var i = 0; i < randomAutoReactEmoji.Length; i++)
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
        await HandleAutoReact(socketMessage);
    }
}