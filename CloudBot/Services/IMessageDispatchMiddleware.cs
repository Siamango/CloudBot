using Discord.WebSocket;

namespace CloudBot.Services;

public interface IMessageDispatchMiddleware
{
    public Task Handle(SocketMessage message);
}