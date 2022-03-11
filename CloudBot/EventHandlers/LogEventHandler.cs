using Discord;
using Discord.WebSocket;

namespace CloudBot.EventHandlers;

public class LogEventHandler : IDiscordClientEventHandler
{
    private readonly ILogger logger;

    public LogEventHandler(ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger("Log");
    }

    public void RegisterHandlers(DiscordSocketClient client) => client.Log += async (message) => await Log(message);

    private Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                logger.LogCritical("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Error:
                logger.LogError("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Warning:
                logger.LogWarning("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Info:
                logger.LogInformation("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Verbose:
                logger.LogTrace("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;

            case LogSeverity.Debug:
                logger.LogDebug("{date,-19} [{severity,8}] {source}: {message} {exception}", DateTime.Now, message.Severity, message.Source, message.Message, message.Exception);
                break;
        }
        return Task.CompletedTask;
    }
}