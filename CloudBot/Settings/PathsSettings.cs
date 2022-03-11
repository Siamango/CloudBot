namespace CloudBot.Settings;

public class PathsSettings
{
    public const string POSITION = "Paths";

    public List<Path> Items { get; init; } = null!;

    public Path? Get(string name) => Items.Find(x => x.Name == name);

    public class Path
    {
        public string Name { get; init; } = null!;

        public string CompletePath { get; init; } = null!;
    }
}