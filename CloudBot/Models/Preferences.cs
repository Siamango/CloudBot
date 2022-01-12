namespace CloudBot;

[Serializable]
public class Preferences
{
    public string WhitelistChannelName { get; set; }

    public string WhitelistConfirmedRoleName { get; set; }

    public string WhitelistPendingRoleName { get; set; }

    public int WhitelistSize { get; set; }

    public Preferences()
    {
        WhitelistChannelName = string.Empty;
        WhitelistConfirmedRoleName = string.Empty;
        WhitelistPendingRoleName = string.Empty;
    }
}