namespace CloudBot.Settings;

public class ConnectionSettings
{
    public const string Position = "Connection";

    public string SolanaEndpoint { get; init; } = null!;

    public ulong GuildId { get; init; }

    public string DiscordToken { get; init; } = null!;
}