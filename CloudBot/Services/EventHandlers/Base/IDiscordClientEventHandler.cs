using Discord.WebSocket;

namespace CloudBot.Services.EventHandlers;

public interface IDiscordClientEventHandler
{
    public void RegisterHandlers(DiscordSocketClient client);
}