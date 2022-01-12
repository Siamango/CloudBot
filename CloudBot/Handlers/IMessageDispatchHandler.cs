using Discord.WebSocket;

namespace CloudBot.Handlers;

public interface IMessageDispatchHandler
{
    public Task<bool> Handle(SocketMessage message);
}