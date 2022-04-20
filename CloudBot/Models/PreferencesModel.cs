namespace CloudBot.Models;

[Serializable]
public class PreferencesModel
{
    public List<ulong> AutoEmojiReactChannels { get; set; }

    public PreferencesModel()
    {
        AutoEmojiReactChannels = new List<ulong>();
    }
}