using CloudBot.CommandModules;
using System.Reflection;

namespace CloudBot.Statics;

public static class Extensions
{
    public static void AddSlashCommandsModules(this IServiceCollection services)
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        var types = currentAssembly.ExportedTypes.Where(x => typeof(ISlashCommandModule).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
        foreach (var type in types)
        {
            services.AddSingleton(typeof(ISlashCommandModule), type);
        }
    }
}