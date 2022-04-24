namespace CloudBot.Services;

public interface IRunner
{
    Task RunAsync(CancellationToken token);
}