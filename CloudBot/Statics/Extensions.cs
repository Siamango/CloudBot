using CloudBot.Services;
using CloudBot.Services.CommandModules;
using CloudBot.Services.EventHandlers;
using Solnet.Rpc;
using System.Reflection;

namespace CloudBot.Statics;

public static class Extensions
{
    public static async Task<bool> IsAddressValid(this IRpcClient client, string address)
    {
        var result = await client.GetBalanceAsync(address);
        return result.WasRequestSuccessfullyHandled;
    }

    public static void AddOfflineRunner<T>(this IServiceCollection services) where T : class, IRunner => _ = services.AddSingleton<IRunner, T>();

    public static Task RunOfflineAsync(this WebApplication app, CancellationToken token)
    {
        var runner = app.Services.GetRequiredService<IRunner>();
        return runner.RunAsync(token);
    }

    public static void AddSlashCommandsModules(this IServiceCollection services)
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        var types = currentAssembly.ExportedTypes.Where(x => typeof(ISlashCommandModule).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
        foreach (var type in types)
        {
            services.AddSingleton(typeof(ISlashCommandModule), type);
            if (typeof(IHostedService).IsAssignableFrom(type))
            {
                services.AddSingleton(typeof(IHostedService), type);
            }
        }
    }

    public static void AddDiscordClientEventHandlers(this IServiceCollection services)
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        var types = currentAssembly.ExportedTypes.Where(x => typeof(IDiscordClientEventHandler).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
        foreach (var type in types)
        {
            services.AddSingleton(typeof(IDiscordClientEventHandler), type);
            if (typeof(IHostedService).IsAssignableFrom(type))
            {
                services.AddSingleton(typeof(IHostedService), type);
            }
        }
    }
}