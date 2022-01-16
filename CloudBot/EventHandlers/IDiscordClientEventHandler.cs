using Discord.WebSocket;

namespace CloudBot.EventHandlers;

public interface IDiscordClientEventHandler
{
    public void RegisterHandlers(DiscordSocketClient client);
}