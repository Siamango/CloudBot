namespace CloudBot.Models;

[Serializable]
public class WhitelistPreferencesModel
{
    public bool InvitesRewardEnabled { get; set; }

    public int InvitesRequired { get; set; }

    public ulong ListenChannelId { get; set; }

    public ulong ConfirmedRoleId { get; set; }

    public ulong PendingRoleId { get; set; }

    public int MaxSize { get; set; }

    public ulong AnnouncementsChannelId { get; set; }

    public WhitelistPreferencesModel()
    {
    }
}