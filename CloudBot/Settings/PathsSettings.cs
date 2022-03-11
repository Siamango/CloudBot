namespace CloudBot.Settings;

public class PathsSettings
{
    public const string POSITION = "Paths";

    public List<Path> Paths { get; init; } = null!;

    public Path? Get(string name) => Paths.Find(x => x.Name == name);

    public class Path
    {
        public string Name { get; init; } = null!;

        public string CompletePath { get; init; } = null!;
    }
}